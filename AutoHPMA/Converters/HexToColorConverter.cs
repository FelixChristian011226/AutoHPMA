using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoHPMA.Converters
{
    /// <summary>
    /// 将十六进制颜色字符串转换为 Color 对象
    /// </summary>
    public class HexToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                try
                {
                    // 确保hex是6位
                    hex = hex.PadRight(6, '0');
                    if (hex.Length > 6)
                        hex = hex.Substring(0, 6);
                    
                    return (Color)ColorConverter.ConvertFromString("#FF" + hex);
                }
                catch
                {
                    // 转换失败返回透明色
                }
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
