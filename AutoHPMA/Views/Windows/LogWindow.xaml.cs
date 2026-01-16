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
using System.Windows.Controls;

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

        private bool _showMarquee = true;
        public bool ShowMarquee
        {
            get => _showMarquee;
            set
            {
                _showMarquee = value;
            }
        }

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

            // 处理ILogger的格式化字符串
            content = content.Replace("{", "").Replace("}", "");

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

        public void RefreshPosition(IntPtr hWnd, int Loffset = 0, int Toffset = 0)
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

        public static void StartMarquee(TextBlock marqueeText, Canvas marqueeCanvas, double viewWidth = 240, double pauseSeconds = 1.5)
        {
            marqueeText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = marqueeText.DesiredSize.Width;
            if (textWidth <= viewWidth)
            {
                Canvas.SetLeft(marqueeText, 0);
                return;
            }
            double scrollSeconds = Math.Max(2, (textWidth - viewWidth) / 60.0);
            var storyboard = new System.Windows.Media.Animation.Storyboard();
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = viewWidth - textWidth,
                BeginTime = TimeSpan.FromSeconds(pauseSeconds),
                Duration = TimeSpan.FromSeconds(scrollSeconds),
                AutoReverse = false,
                FillBehavior = System.Windows.Media.Animation.FillBehavior.HoldEnd
            };
            System.Windows.Media.Animation.Storyboard.SetTarget(animation, marqueeText);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
            storyboard.Children.Add(animation);
            storyboard.Completed += (s, e) =>
            {
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(pauseSeconds);
                timer.Tick += (s2, e2) =>
                {
                    timer.Stop();
                    Canvas.SetLeft(marqueeText, 0);
                    StartMarquee(marqueeText, marqueeCanvas, viewWidth, pauseSeconds);
                };
                timer.Start();
            };
            storyboard.Begin();
        }

        private void MarqueeText_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_showMarquee) return;

            var textBlock = sender as TextBlock;
            if (textBlock == null) return;
            var parent = VisualTreeHelper.GetParent(textBlock);
            while (parent != null && !(parent is Canvas))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            var canvas = parent as Canvas;
            if (canvas != null)
            {
                StartMarquee(textBlock, canvas, 240, 1.5);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        /// <summary>
        /// 将窗口置于所有 Topmost 窗口的最上层
        /// </summary>
        public void BringToFront()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }

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
