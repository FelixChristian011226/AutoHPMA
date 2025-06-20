using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace AutoHPMA.Converters
{
    public class KeyToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Tuple<ModifierKeys, Key> tuple)
            {
                string mod = "";
                if ((tuple.Item1 & ModifierKeys.Control) != 0) mod += "Ctrl+";
                if ((tuple.Item1 & ModifierKeys.Alt) != 0) mod += "Alt+";
                if ((tuple.Item1 & ModifierKeys.Shift) != 0) mod += "Shift+";
                if ((tuple.Item1 & ModifierKeys.Windows) != 0) mod += "Win+";
                return mod + tuple.Item2.ToString();
            }
            if (value is Key key)
            {
                return key.ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 