using Serilog.Core;
using Serilog.Events;
using AutoHPMA.Views.Windows;
using AutoHPMA.Services;

namespace AutoHPMA.Helpers
{
    public class LogWindowSink : ILogEventSink
    {
        private readonly IFormatProvider _formatProvider;

        public LogWindowSink(IFormatProvider formatProvider = null)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            var logWindow = AppContextService.Instance.LogWindow;
            if (logWindow == null) return;

            string category = GetCategoryFromLogLevel(logEvent.Level);
            string message = logEvent.RenderMessage(_formatProvider);

            logWindow.AddLogMessage(category, message);
        }

        private string GetCategoryFromLogLevel(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => "VRB",         //LogTrace
                LogEventLevel.Debug => "DBG",           //LogDebug
                LogEventLevel.Information => "INF",     //LogInformation
                LogEventLevel.Warning => "WRN",         //LogWarning
                LogEventLevel.Error => "ERR",           //LogError
                LogEventLevel.Fatal => "FAT",           //LogCritical
                _ => "INF"
            };
        }
    }
}