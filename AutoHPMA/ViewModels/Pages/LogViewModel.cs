using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Text;
using Serilog.Core;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using AutoHPMA.Views.Pages;

namespace AutoHPMA.ViewModels.Pages
{
    public partial class LogViewModel : ObservableObject
    {
        private readonly ILogger<LogViewModel> _logger;
        private readonly StringBuilder _logBuilder = new();
        private readonly ObservableCollection<LogEvent> _logEvents = new();

        public static LogViewModel Instance { get; private set; }

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

        [ObservableProperty]
        private string _logText = string.Empty;

        public LogViewModel(ILogger<LogViewModel> logger)
        {
            _logger = logger;
            Instance = this;
            _logger.LogInformation("日志系统初始化完成");

            // 处理之前暂存的日志
            if (LogEventSink.Instance != null)
            {
                LogEventSink.Instance.ProcessPendingLogs();
            }
        }

        public void AddLogEvent(LogEvent logEvent)
        {
            if (!ShouldShowLog(logEvent.Level))
                return;

            _logEvents.Add(logEvent);
            UpdateLogText();
        }

        private bool ShouldShowLog(LogEventLevel level)
        {
            return level switch
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

        private void UpdateLogText()
        {
            _logBuilder.Clear();
            foreach (var logEvent in _logEvents)
            {
                if (ShouldShowLog(logEvent.Level))
                {
                    _logBuilder.AppendLine($"[{logEvent.Timestamp:HH:mm:ss}] [{logEvent.Level}] {logEvent.RenderMessage()}");
                }
            }
            LogText = _logBuilder.ToString();
            
            // 触发滚动到底部的事件
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (LogPage.Instance?.LogScrollViewer != null)
                {
                    LogPage.Instance.LogScrollViewer.ScrollToBottom();
                }
            }));
        }

        [RelayCommand]
        private void ClearLogs()
        {
            _logEvents.Clear();
            UpdateLogText();
        }

        partial void OnShowVerboseChanged(bool value) => UpdateLogText();
        partial void OnShowInfoChanged(bool value) => UpdateLogText();
        partial void OnShowDebugChanged(bool value) => UpdateLogText();
        partial void OnShowWarningChanged(bool value) => UpdateLogText();
        partial void OnShowErrorChanged(bool value) => UpdateLogText();
        partial void OnShowFatalChanged(bool value) => UpdateLogText();
    }

    public class LogEventSink : ILogEventSink
    {
        public static LogEventSink Instance { get; private set; }
        private readonly Queue<LogEvent> _pendingLogEvents = new Queue<LogEvent>();

        public LogEventSink()
        {
            Instance = this;
        }

        public void Emit(LogEvent logEvent)
        {
            if (LogViewModel.Instance != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogViewModel.Instance.AddLogEvent(logEvent);
                });
            }
            else
            {
                // 如果LogViewModel实例还不存在，将日志事件暂存
                _pendingLogEvents.Enqueue(logEvent);
            }
        }

        public void ProcessPendingLogs()
        {
            if (LogViewModel.Instance == null) return;

            while (_pendingLogEvents.Count > 0)
            {
                var logEvent = _pendingLogEvents.Dequeue();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogViewModel.Instance.AddLogEvent(logEvent);
                });
            }
        }
    }
} 