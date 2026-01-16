using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using AutoHPMA.Models;
using AutoHPMA.Services;

namespace AutoHPMA.ViewModels.Pages;

public partial class LogViewModel : ObservableObject
{
    private readonly ILogger<LogViewModel> _logger;
    private readonly ICollectionView _filteredLogsView;

    [ObservableProperty]
    private bool _showVerbose = true;

    [ObservableProperty]
    private bool _showInfo = true;

    [ObservableProperty]
    private bool _showDebug = true;

    [ObservableProperty]
    private bool _showWarning = true;

    [ObservableProperty]
    private bool _showError = true;

    [ObservableProperty]
    private bool _showFatal = true;

    /// <summary>
    /// 日志的筛选视图，绑定到 UI
    /// </summary>
    public ICollectionView FilteredLogs => _filteredLogsView;

    /// <summary>
    /// 滚动到底部事件，View 订阅此事件
    /// </summary>
    public event Action? ScrollToBottomRequested;

    public LogViewModel(ILogger<LogViewModel> logger)
    {
        _logger = logger;

        // 使用 LogService 的统一数据源
        _filteredLogsView = CollectionViewSource.GetDefaultView(LogService.Instance.AllLogs);
        _filteredLogsView.Filter = FilterLog;

        // 订阅日志添加事件，触发滚动
        LogService.Instance.LogAdded += OnLogAdded;

        _logger.LogInformation("日志系统初始化完成");

    }

    private void OnLogAdded(LogEntry entry)
    {
        // 刷新筛选视图并请求滚动
        _filteredLogsView.Refresh();
        ScrollToBottomRequested?.Invoke();
    }

    private bool FilterLog(object obj)
    {
        if (obj is not LogEntry entry) return false;

        return entry.Level switch
        {
            LogEventLevel.Verbose => ShowVerbose,
            LogEventLevel.Information => ShowInfo,
            LogEventLevel.Debug => ShowDebug,
            LogEventLevel.Warning => ShowWarning,
            LogEventLevel.Error => ShowError,
            LogEventLevel.Fatal => ShowFatal,
            _ => true
        };
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogService.Instance.ClearLogs();
    }

    partial void OnShowVerboseChanged(bool value) => _filteredLogsView.Refresh();
    partial void OnShowInfoChanged(bool value) => _filteredLogsView.Refresh();
    partial void OnShowDebugChanged(bool value) => _filteredLogsView.Refresh();
    partial void OnShowWarningChanged(bool value) => _filteredLogsView.Refresh();
    partial void OnShowErrorChanged(bool value) => _filteredLogsView.Refresh();
    partial void OnShowFatalChanged(bool value) => _filteredLogsView.Refresh();
}
