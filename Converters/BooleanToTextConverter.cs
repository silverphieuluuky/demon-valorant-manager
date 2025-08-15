using System;
using System.Globalization;
using System.Windows.Data;

namespace RiotAutoLogin.Converters
{
    public class BooleanToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "ON" : "OFF";
            }
            return "OFF";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue.Equals("ON", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
} 