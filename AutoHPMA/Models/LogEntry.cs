using Serilog.Events;
using System;

namespace AutoHPMA.Models;

/// <summary>
/// 统一的日志条目模型
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogEventLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }

    /// <summary>
    /// 获取日志级别的简写（用于 LogWindow 显示）
    /// </summary>
    public string LevelShort => Level switch
    {
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "FAT",
        _ => "INF"
    };

    /// <summary>
    /// 从 Serilog LogEvent 创建 LogEntry
    /// </summary>
    public static LogEntry FromLogEvent(LogEvent logEvent, IFormatProvider? formatProvider = null)
    {
        return new LogEntry
        {
            Timestamp = logEvent.Timestamp.LocalDateTime,
            Level = logEvent.Level,
            Message = logEvent.RenderMessage(formatProvider),
            Exception = logEvent.Exception
        };
    }
}
