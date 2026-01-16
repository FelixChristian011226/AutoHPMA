using AutoHPMA.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace AutoHPMA.Services;

/// <summary>
/// 中央日志服务 - 管理所有日志数据的单一数据源
/// </summary>
public class LogService : INotifyPropertyChanged
{
    private static readonly Lazy<LogService> _instance = new(() => new LogService());
    public static LogService Instance => _instance.Value;

    /// <summary>
    /// 所有日志的主数据源（用于 LogPage 显示和筛选）
    /// </summary>
    public ObservableCollection<LogEntry> AllLogs { get; } = new();

    /// <summary>
    /// 新日志添加事件，LogWindow 订阅此事件获取日志通知
    /// </summary>
    public event Action<LogEntry>? LogAdded;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LogService() { }

    /// <summary>
    /// 添加日志条目
    /// </summary>
    public void AddLog(LogEntry entry)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AllLogs.Add(entry);
            LogAdded?.Invoke(entry);
        });
    }

    /// <summary>
    /// 清除界面日志（不影响文件日志）
    /// </summary>
    public void ClearLogs()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            AllLogs.Clear();
        });
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
