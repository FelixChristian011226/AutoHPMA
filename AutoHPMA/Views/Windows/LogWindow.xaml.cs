using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace AutoHPMA.Views.Windows
{
    public partial class LogWindow : Window
    {
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int GWL_EXSTYLE = -20;

        public LogWindow()
        {
            InitializeComponent();
            SetWindowStyle();
        }

        public static LogWindow Instance()
        {
            var instance = new LogWindow();
            instance.Show();
            // 设置窗口位置为左下角
            instance.Left = 0;
            instance.Top = 0;
            return instance;
        }

        public void RefreshPosition(IntPtr hWnd)
        {
            var rect = new RECT();
            if (GetWindowRect(hWnd, out rect))
            {
                Left = rect.Left;
                Top = rect.Top;
                //Width = rect.Right - rect.Left;
                //Height = rect.Bottom - rect.Top;
            }
        }

        private void SetWindowStyle()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            Background = Brushes.Transparent;
            Topmost = true;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
