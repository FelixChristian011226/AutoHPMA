using System;

namespace AutoHPMA.Models;

public class WindowInfo
{
    public nint Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    
    public string DisplayName => string.IsNullOrEmpty(Title) ? $"{ProcessName} (PID: {ProcessId})" : $"{Title} - {ProcessName}";
    
    public override string ToString() => DisplayName;
    
    public override bool Equals(object? obj)
    {
        return obj is WindowInfo other && Handle == other.Handle;
    }
    
    public override int GetHashCode()
    {
        return Handle.GetHashCode();
    }
}