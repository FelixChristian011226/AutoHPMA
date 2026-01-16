using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Serilog.Events;

namespace AutoHPMA.Converters;

/// <summary>
/// 将 Serilog LogEventLevel 转换为对应颜色
/// </summary>
public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Verbose => Brushes.LightGray,
                LogEventLevel.Debug => Brushes.LightGreen,
                LogEventLevel.Information => Brushes.LightBlue,
                LogEventLevel.Warning => Brushes.Yellow,
                LogEventLevel.Error => Brushes.Red,
                LogEventLevel.Fatal => Brushes.DarkRed,
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
