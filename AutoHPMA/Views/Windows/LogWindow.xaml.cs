using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AutoHPMA.Helpers;
using AutoHPMA.Models;

namespace AutoHPMA.Views.Windows;

public partial class LogWindow : Window, INotifyPropertyChanged
{
    #region 常量

    private const int MaxLogCount = 100;

    #endregion

    #region 字段

    private readonly ObservableCollection<LogMessage> _logMessages = new();
    private readonly DispatcherTimer _timeTimer;

    private bool _showMarquee = true;
    private bool _showDebugLogs = false;
    private string _timeNow = string.Empty;
    private string _currentGameState = "空闲";

    #endregion

    #region 属性

    public bool ShowMarquee
    {
        get => _showMarquee;
        set
        {
            if (_showMarquee != value)
            {
                _showMarquee = value;
                OnPropertyChanged(nameof(ShowMarquee));
            }
        }
    }

    public bool ShowDebugLogs
    {
        get => _showDebugLogs;
        set
        {
            if (_showDebugLogs != value)
            {
                _showDebugLogs = value;
                OnPropertyChanged(nameof(ShowDebugLogs));
                FilterLogMessages();
            }
        }
    }

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

    #endregion

    #region 事件

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

    #endregion

    #region 构造函数

    public LogWindow()
    {
        InitializeComponent();
        DataContext = this;

        _timeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timeTimer.Tick += (_, _) => TimeNow = DateTime.Now.ToString("HH:mm:ss");
        _timeTimer.Start();

        LogListBox.ItemsSource = _logMessages;
        Loaded += LogWindow_Loaded;
        Closing += LogWindow_Closing;
    }

    #endregion

    #region 事件处理

    private void LogWindow_Loaded(object sender, RoutedEventArgs e)
    {
        NativeWindowHelper.SetWindowTransparent(this);
        Background = Brushes.Transparent;
        Topmost = true;
    }

    private void LogWindow_Closing(object? sender, CancelEventArgs e)
    {
        _timeTimer.Stop();
    }

    private void MarqueeText_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_showMarquee) return;

        if (sender is not TextBlock textBlock) return;
        
        var parent = VisualTreeHelper.GetParent(textBlock);
        while (parent != null && parent is not Canvas)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        
        if (parent is Canvas canvas)
        {
            StartMarquee(textBlock, canvas, 230, 1.5);
        }
    }

    #endregion

    #region 公共方法

    public void SetGameState(string state) => CurrentGameState = state;

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

        // 限制日志数量，移除最旧的
        while (_logMessages.Count > MaxLogCount)
        {
            _logMessages.RemoveAt(0);
        }

        ScrollToBottom();
    }

    public void DeleteLastLogMessage()
    {
        if (_logMessages.Count > 0)
        {
            _logMessages.RemoveAt(_logMessages.Count - 1);
        }
    }

    public void RefreshPosition(IntPtr hWnd, int offsetX = 0, int offsetY = 0)
    {
        if (NativeWindowHelper.GetWindowPosition(hWnd, this, out double left, out double top, out _, out _))
        {
            Left = left + offsetX;
            Top = top + offsetY;
            ScrollToBottom();
        }
    }

    public void BringToFront() => NativeWindowHelper.BringWindowToFront(this);

    #endregion

    #region 私有方法

    private void ScrollToBottom()
    {
        if (LogListBox.Items.Count > 0)
        {
            LogListBox.ScrollIntoView(LogListBox.Items[^1]);
        }
    }

    private void FilterLogMessages()
    {
        // 使用临时列表存储需要保留的消息
        var toKeep = new System.Collections.Generic.List<LogMessage>();
        
        foreach (var log in _logMessages)
        {
            if (log.Category != "DBG" || _showDebugLogs)
            {
                toKeep.Add(log);
            }
        }

        _logMessages.Clear();
        foreach (var msg in toKeep)
        {
            _logMessages.Add(msg);
        }
    }

    private static void StartMarquee(TextBlock marqueeText, Canvas marqueeCanvas, double viewWidth = 230, double pauseSeconds = 1.5)
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
        
        storyboard.Completed += (_, _) =>
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(pauseSeconds) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Canvas.SetLeft(marqueeText, 0);
                StartMarquee(marqueeText, marqueeCanvas, viewWidth, pauseSeconds);
            };
            timer.Start();
        };
        
        storyboard.Begin();
    }

    #endregion
}
