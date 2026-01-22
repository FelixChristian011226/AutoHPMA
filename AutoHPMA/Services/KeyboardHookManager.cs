using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace AutoHPMA.Services
{
    /// <summary>
    /// 条件键盘钩子管理器 - 仅当目标窗口在前台时触发热键
    /// </summary>
    public class KeyboardHookManager : IDisposable
    {
        #region 事件

        /// <summary>
        /// 热键触发事件
        /// </summary>
        public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

        #endregion

        #region 私有字段

        private IntPtr _hookId = IntPtr.Zero;
        private IntPtr _targetWindowHandle = IntPtr.Zero;
        private readonly Dictionary<string, HotkeyRegistration> _registrations = new();
        private readonly LowLevelKeyboardProc _hookProc;
        private bool _disposed;

        #endregion

        #region 构造函数

        public KeyboardHookManager()
        {
            // 保持对委托的引用，防止被垃圾回收
            _hookProc = HookCallback;
            _hookId = SetHook(_hookProc);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置目标窗口句柄，只有该窗口在前台时热键才生效
        /// </summary>
        public void SetTargetWindow(IntPtr hwnd)
        {
            _targetWindowHandle = hwnd;
        }

        /// <summary>
        /// 注册热键
        /// </summary>
        /// <param name="actionName">热键对应的操作名称</param>
        /// <param name="modifiers">修饰键</param>
        /// <param name="key">按键</param>
        public void RegisterHotkey(string actionName, ModifierKeys modifiers, Key key)
        {
            if (key == Key.None)
            {
                // 如果 key 为 None，则移除该热键
                _registrations.Remove(actionName);
                return;
            }

            _registrations[actionName] = new HotkeyRegistration
            {
                ActionName = actionName,
                Modifiers = modifiers,
                Key = key,
                VirtualKeyCode = KeyInterop.VirtualKeyFromKey(key)
            };
        }

        /// <summary>
        /// 注销指定热键
        /// </summary>
        public void UnregisterHotkey(string actionName)
        {
            _registrations.Remove(actionName);
        }

        /// <summary>
        /// 注销所有热键
        /// </summary>
        public void UnregisterAll()
        {
            _registrations.Clear();
        }

        #endregion

        #region 私有方法

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                // 检查目标窗口是否在前台
                var foregroundWindow = GetForegroundWindow();
                if (_targetWindowHandle != IntPtr.Zero && foregroundWindow == _targetWindowHandle)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    var currentModifiers = GetCurrentModifiers();

                    // 检查是否匹配已注册的热键
                    foreach (var registration in _registrations.Values)
                    {
                        if (registration.VirtualKeyCode == vkCode && registration.Modifiers == currentModifiers)
                        {
                            // 触发热键事件
                            HotkeyPressed?.Invoke(this, new HotkeyEventArgs(registration.Modifiers, registration.Key, registration.ActionName));
                            
                            // 拦截按键，不传递给其他程序
                            return (IntPtr)1;
                        }
                    }
                }
            }

            // 按键传递给其他程序
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static ModifierKeys GetCurrentModifiers()
        {
            ModifierKeys modifiers = ModifierKeys.None;

            if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
                modifiers |= ModifierKeys.Control;
            if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0)   // VK_MENU = Alt
                modifiers |= ModifierKeys.Alt;
            if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
                modifiers |= ModifierKeys.Shift;
            if ((GetAsyncKeyState(VK_LWIN) & 0x8000) != 0 || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0)
                modifiers |= ModifierKeys.Windows;

            return modifiers;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_hookId != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }

                if (disposing)
                {
                    _registrations.Clear();
                }

                _disposed = true;
            }
        }

        ~KeyboardHookManager()
        {
            Dispose(false);
        }

        #endregion

        #region P/Invoke 声明

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12;    // Alt
        private const int VK_SHIFT = 0x10;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        #endregion

        #region 内部类

        private class HotkeyRegistration
        {
            public string ActionName { get; set; } = string.Empty;
            public ModifierKeys Modifiers { get; set; }
            public Key Key { get; set; }
            public int VirtualKeyCode { get; set; }
        }

        #endregion
    }

    /// <summary>
    /// 热键事件参数
    /// </summary>
    public class HotkeyEventArgs : EventArgs
    {
        public ModifierKeys Modifiers { get; }
        public Key Key { get; }
        public string ActionName { get; }

        public HotkeyEventArgs(ModifierKeys modifiers, Key key, string actionName = "")
        {
            Modifiers = modifiers;
            Key = key;
            ActionName = actionName;
        }
    }
}
