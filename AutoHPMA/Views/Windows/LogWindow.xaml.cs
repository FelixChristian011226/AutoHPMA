using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using AutoHPMA.Helpers;
using AutoHPMA.Models;
using AutoHPMA.Services;
using Serilog.Events;

namespace AutoHPMA.Views.Windows;

public partial class LogWindow : Window, INotifyPropertyChanged
{
    #region 常量

    private const int MaxLogCount = 100;

    #endregion

    #region 字段

    private readonly ObservableCollection<LogEntry> _logMessages = new();
    private readonly ICollectionView _filteredLogsView;
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
                // 使用 CollectionViewSource 刷新筛选
                _filteredLogsView.Refresh();
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

    /// <summary>
    /// 筛选后的日志视图，绑定到 UI
    /// </summary>
    public ICollectionView FilteredLogs => _filteredLogsView;

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

        // 初始化 CollectionViewSource 用于筛选
        _filteredLogsView = CollectionViewSource.GetDefaultView(_logMessages);
        _filteredLogsView.Filter = FilterLog;

        _timeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timeTimer.Tick += (_, _) => TimeNow = DateTime.Now.ToString("HH:mm:ss");
        _timeTimer.Start();

        LogListBox.ItemsSource = _filteredLogsView;
        
        // 订阅 LogService 的日志添加事件
        LogService.Instance.LogAdded += OnLogAdded;

        Loaded += LogWindow_Loaded;
        Closing += LogWindow_Closing;
    }

    #endregion

    #region 事件处理

    private void OnLogAdded(LogEntry entry)
    {
        Dispatcher.Invoke(() =>
        {
            // 添加到本地集合（LogWindow 有自己的日志上限）
            _logMessages.Add(entry);

            // 限制日志数量，移除最旧的
            while (_logMessages.Count > MaxLogCount)
            {
                _logMessages.RemoveAt(0);
            }

            ScrollToBottom();
        });
    }

    private bool FilterLog(object obj)
    {
        if (obj is not LogEntry entry) return false;

        // 如果不显示 Debug 日志且当前是 Debug 级别，则过滤掉
        if (!_showDebugLogs && entry.Level == LogEventLevel.Debug)
            return false;

        return true;
    }

    private void LogWindow_Loaded(object sender, RoutedEventArgs e)
    {
        NativeWindowHelper.SetWindowTransparent(this);
        Background = Brushes.Transparent;
        Topmost = true;
    }

    private void LogWindow_Closing(object? sender, CancelEventArgs e)
    {
        _timeTimer.Stop();
        LogService.Instance.LogAdded -= OnLogAdded;
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
