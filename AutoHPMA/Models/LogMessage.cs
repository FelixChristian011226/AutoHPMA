using System;

namespace AutoHPMA.Models;

/// <summary>
/// 日志消息模型
/// </summary>
public class LogMessage
{
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
