using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AutoHPMA.Converters;

/// <summary>
/// 将布尔值转换为 Visibility 值
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 是否反转逻辑（true -> Collapsed, false -> Visible）
    /// </summary>
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;

        if (Invert)
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = value is Visibility visibility && visibility == Visibility.Visible;

        if (Invert)
            isVisible = !isVisible;

        return isVisible;
    }
}
