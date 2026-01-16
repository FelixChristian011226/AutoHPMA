using System;
using System.Globalization;
using System.Windows.Data;
using Serilog.Events;

namespace AutoHPMA.Converters;

/// <summary>
/// 将 Serilog LogEventLevel 转换为显示文本
/// </summary>
public class LogLevelToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogEventLevel level)
        {
            return $"[{level}]";
        }
        return "[Unknown]";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
