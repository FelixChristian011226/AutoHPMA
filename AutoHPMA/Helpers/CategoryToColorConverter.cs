using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AutoHPMA.Helpers
{
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string category = value as string;
            switch (category)
            {
                case "WRN":
                    return Brushes.Orange;
                case "ERR":
                    return Brushes.Red;
                case "INF":
                    return Brushes.DarkCyan;
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
