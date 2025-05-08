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
using System.ComponentModel;
using System.Windows.Threading;

namespace AutoHPMA.Views.Windows
{
    public class LogMessage
    {
        public DateTime Timestamp { get; set; }
        public string Category { get; set; }
        public string Content { get; set; }
    }

    public partial class LogWindow : Window, INotifyPropertyChanged
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        private ObservableCollection<LogMessage> _logMessages = new ObservableCollection<LogMessage>();

        private bool _showDebugLogs = false;
        public bool ShowDebugLogs
        {
            get => _showDebugLogs;
            set
            {
                _showDebugLogs = value;
                FilterLogMessages(); // 当属性修改时，更新日志列表
            }
        }

        private string _timeNow;
        public string TimeNow
        {
            get => _timeNow;
            set
            {
                if (_timeNow != value)
                {
                    _timeNow = value;
                    OnPropertyChanged(nameof(TimeNow));
                }
            }
        }

        private string _currentGameState = "空闲";
        public string CurrentGameState
        {
            get => _currentGameState;
            set
            {
                if (_currentGameState != value)
                {
                    _currentGameState = value;
                    OnPropertyChanged(nameof(CurrentGameState));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        public void SetGameState(string state)
        {
            CurrentGameState = state;
        }

        public LogWindow()
        {
            InitializeComponent();
            DataContext = this;

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) =>
            {
                TimeNow = DateTime.Now.ToString("HH:mm:ss");
            };
            timer.Start();

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
            if (category == "DBG" && !_showDebugLogs)
                return;

            var message = new LogMessage
            {
                Timestamp = DateTime.Now,
                Category = category,
                Content = content
            };

            _logMessages.Add(message);

            // 自动滚动
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

        public void RefreshPosition(IntPtr hWnd, int Loffset, int Toffset)
        {
            var rect = new RECT();
            if (GetWindowRect(hWnd, out rect))
            {
                // 获取当前窗口的DPI缩放比例
                PresentationSource source = PresentationSource.FromVisual(this);
                if (source?.CompositionTarget != null)
                {
                    double dpiX = source.CompositionTarget.TransformFromDevice.M11;
                    double dpiY = source.CompositionTarget.TransformFromDevice.M22;

                    Left = rect.Left * dpiX + Loffset;
                    Top = rect.Top * dpiY + Toffset;
                }
                else
                {
                    Left = rect.Left + Loffset;
                    Top = rect.Top + Toffset;
                }
                if (LogListBox.Items.Count > 0)
                {
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                }
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

        private void FilterLogMessages()
        {
            var filteredMessages = new ObservableCollection<LogMessage>();
            foreach (var log in _logMessages)
            {
                if (log.Category == "DBG" && !_showDebugLogs)
                    continue;

                filteredMessages.Add(log);
            }

            _logMessages.Clear();
            foreach (var msg in filteredMessages)
            {
                _logMessages.Add(msg);
            }
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
