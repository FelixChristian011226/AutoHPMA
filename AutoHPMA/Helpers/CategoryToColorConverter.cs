using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoHPMA.Helpers
{
    /// <summary>
    /// 颜色转换器
    /// 用于将日志消息的类别转换为不同颜色
    /// </summary>
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string category = value as string;
            switch (category)
            {
                case "WRN":
                    return Brushes.Yellow;
                case "ERR":
                    return Brushes.Red;
                case "INF":
                    return Brushes.Cyan;
                case "DBG":
                    return Brushes.Orange;
                default:
                    return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
