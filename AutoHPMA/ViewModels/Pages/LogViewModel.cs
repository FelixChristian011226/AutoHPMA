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
        public LogEventSink()
        {
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
        }
    }
} 