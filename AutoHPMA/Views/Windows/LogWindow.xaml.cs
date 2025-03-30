using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AutoHPMA.Helpers;

namespace AutoHPMA.Views.Windows
{
    public class LogMessage
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }
        public string Content { get; set; }
    }

    public partial class LogWindow : Window
    {
        private static LogWindow? _instance;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        private ObservableCollection<LogMessage> _logMessages = new ObservableCollection<LogMessage>();

        public LogWindow()
        {
            InitializeComponent();
            // 绑定日志消息集合到TextBlock
            LogListBox.ItemsSource = _logMessages;
            Loaded += LogWindow_Loaded;
        }

        private void LogWindow_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            Background = Brushes.Transparent;
            Topmost = true;
        }

        // 添加日志消息
        public void AddLogMessage(string category, string content)
        {
            var message = new LogMessage
            {
                Timestamp = DateTime.Now,
                Category = category,
                Content = content
            };
            _logMessages.Add(message);
            //自动滚动
            if (LogListBox.Items.Count > 0)
            {
                LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
            }
        }

        public void DeleteLastLogMessage()
        {
            if (_logMessages.Count > 0)
            {
                _logMessages.RemoveAt(_logMessages.Count - 1);
            }
        }

        public static LogWindow GetInstance()
        {
            //if (_instance == null || !_instance.IsLoaded)
            //{
            //    _instance = new LogWindow();
            //}
            return _instance;
        }

        public static LogWindow Instance()
        {
            var instance = new LogWindow();
            instance.Show();
            // 设置窗口位置为左上角
            instance.Left = 0;
            instance.Top = 0;
            _instance = instance;
            return _instance;
        }

        public void RefreshPosition(IntPtr hWnd, int Loffset, int Toffset)
        {
            var rect = new RECT();
            if (GetWindowRect(hWnd, out rect))
            {
                Left = rect.Left+ Loffset;
                Top = rect.Top+ Toffset;
                //Width = rect.Right - rect.Left;
                //Height = rect.Bottom - rect.Top;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        public void HideLogWindow()
        {
            this.Visibility = Visibility.Collapsed;
        }

        public void ShowLogWindow()
        {
            this.Visibility = Visibility.Visible;
        }

        public void ToggleLogWindowVisibility(bool isVisible)
        {
            this.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
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
