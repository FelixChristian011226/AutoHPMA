using Serilog.Core;
using Serilog.Events;
using AutoHPMA.Models;
using AutoHPMA.Services;

namespace AutoHPMA.Helpers.LogHelper;

/// <summary>
/// 统一的日志 Sink，将日志分发到 LogService
/// 替代原来分散的 LogWindowSink 和 LogEventSink
/// </summary>
public class LogServiceSink : ILogEventSink
{
    private readonly IFormatProvider? _formatProvider;

    public LogServiceSink(IFormatProvider? formatProvider = null)
    {
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        var entry = LogEntry.FromLogEvent(logEvent, _formatProvider);
        LogService.Instance.AddLog(entry);
    }
}
