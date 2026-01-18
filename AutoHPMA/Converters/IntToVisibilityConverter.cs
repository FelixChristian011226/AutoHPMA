using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoHPMA.Converters;

/// <summary>
/// 将整数转换为 Visibility 值（大于 0 为 Visible，否则为 Collapsed）
/// </summary>
public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
        {
            return intValue > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
