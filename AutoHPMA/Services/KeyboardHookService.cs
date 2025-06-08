using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using AutoHPMA.ViewModels.Pages;
using Microsoft.Extensions.Logging;

namespace AutoHPMA.Services
{
    public class KeyboardHookService : IDisposable
    {
        private readonly ILogger<KeyboardHookService> _logger;
        private readonly HotkeySettingsViewModel _hotkeySettings;
        private IntPtr _hookHandle = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private readonly ConcurrentQueue<string> _actionQueue = new();
        private System.Windows.Threading.DispatcherTimer _actionTimer;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;

        public KeyboardHookService(ILogger<KeyboardHookService> logger, HotkeySettingsViewModel hotkeySettings)
        {
            _logger = logger;
            _hotkeySettings = hotkeySettings;
            SetupKeyboardHook();
            SetupActionTimer();
        }

        private void SetupActionTimer()
        {
            _actionTimer = new System.Windows.Threading.DispatcherTimer();
            _actionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _actionTimer.Tick += ActionTimer_Tick;
            _actionTimer.Start();
        }

        private void ActionTimer_Tick(object sender, EventArgs e)
        {
            while (_actionQueue.TryDequeue(out string actionName))
            {
                try
                {
                    _logger.LogDebug($"Executing hotkey action: {actionName}");
                    _hotkeySettings?.ExecuteHotkeyAction(actionName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing hotkey action: {actionName}");
                }
            }
        }

        private void SetupKeyboardHook()
        {
            _proc = HookCallback;
            _hookHandle = SetHook(_proc);
            if (_hookHandle == IntPtr.Zero)
            {
                _logger.LogError("Failed to set keyboard hook");
            }
            else
            {
                _logger.LogDebug("Keyboard hook set successfully");
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Key key = KeyInterop.KeyFromVirtualKey(vkCode);
                _logger.LogDebug($"Key pressed: {key}");

                if (_hotkeySettings != null)
                {
                    var binding = _hotkeySettings.HotkeyBindings.FirstOrDefault(b => b.Key == key);
                    if (binding != null)
                    {
                        _logger.LogDebug($"Found matching hotkey binding: {binding.Name}");
                        _actionQueue.Enqueue(binding.Name);
                    }
                }
            }
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
            _actionTimer?.Stop();
        }
    }
} 