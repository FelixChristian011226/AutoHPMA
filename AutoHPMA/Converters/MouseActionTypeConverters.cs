using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoHPMA.Models;

namespace AutoHPMA.Converters;

/// <summary>
/// 将 MouseActionType 转换为对应的颜色
/// </summary>
public class MouseActionTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MouseActionType actionType)
        {
            return actionType switch
            {
                MouseActionType.Click => new SolidColorBrush(Color.FromRgb(59, 130, 246)),      // 蓝色
                MouseActionType.Drag => new SolidColorBrush(Color.FromRgb(34, 197, 94)),       // 绿色
                MouseActionType.LongPress => new SolidColorBrush(Color.FromRgb(249, 115, 22)), // 橙色
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 将 MouseActionType 转换为对应的浅色背景
/// </summary>
public class MouseActionTypeToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MouseActionType actionType)
        {
            return actionType switch
            {
                MouseActionType.Click => new SolidColorBrush(Color.FromArgb(30, 59, 130, 246)),      // 浅蓝色
                MouseActionType.Drag => new SolidColorBrush(Color.FromArgb(30, 34, 197, 94)),       // 浅绿色
                MouseActionType.LongPress => new SolidColorBrush(Color.FromArgb(30, 249, 115, 22)), // 浅橙色
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 将 null 转换为 Collapsed，非 null 转换为 Visible
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 拖拽类型时显示（终点坐标）
/// </summary>
public class ActionTypeToDragVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MouseActionType actionType)
        {
            return actionType == MouseActionType.Drag ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 拖拽或长按类型时显示（持续时间）
/// </summary>
public class ActionTypeToDurationVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MouseActionType actionType)
        {
            return actionType == MouseActionType.Drag || actionType == MouseActionType.LongPress 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 点击或长按类型时显示（重复次数）
/// </summary>
public class ActionTypeToTimesVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MouseActionType actionType)
        {
            return actionType == MouseActionType.Click || actionType == MouseActionType.LongPress 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
