using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace AutoHPMA.Services
{
    public class HotkeyManager : IDisposable
    {
        public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

        private readonly Dictionary<int, HotkeyRegistration> _registrations = new();
        private HwndSource? _source;
        private int _currentId = 0;

        public HotkeyManager(IntPtr hwnd)
        {
            _source = HwndSource.FromHwnd(hwnd);
            if (_source != null)
                _source.AddHook(WndProc);
        }

        public int RegisterHotKey(ModifierKeys modifiers, Key key)
        {
            _currentId++;
            if (!RegisterHotKey(_source?.Handle ?? IntPtr.Zero, _currentId, (uint)modifiers, (uint)KeyInterop.VirtualKeyFromKey(key)))
                throw new InvalidOperationException("Hotkey registration failed");
            _registrations[_currentId] = new HotkeyRegistration { Modifiers = modifiers, Key = key };
            return _currentId;
        }

        public void UnregisterHotKey(int id)
        {
            if (_registrations.ContainsKey(id))
            {
                UnregisterHotKey(_source?.Handle ?? IntPtr.Zero, id);
                _registrations.Remove(id);
            }
        }

        public void UnregisterAll()
        {
            foreach (var id in new List<int>(_registrations.Keys))
                UnregisterHotKey(id);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_registrations.TryGetValue(id, out var reg))
                {
                    HotkeyPressed?.Invoke(this, new HotkeyEventArgs(reg.Modifiers, reg.Key));
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterAll();
            if (_source != null)
                _source.RemoveHook(WndProc);
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private class HotkeyRegistration
        {
            public ModifierKeys Modifiers { get; set; }
            public Key Key { get; set; }
        }
    }

    public class HotkeyEventArgs : EventArgs
    {
        public ModifierKeys Modifiers { get; }
        public Key Key { get; }
        public HotkeyEventArgs(ModifierKeys modifiers, Key key)
        {
            Modifiers = modifiers;
            Key = key;
        }
    }
} 