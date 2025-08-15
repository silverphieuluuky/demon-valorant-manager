using System;
using System.Globalization;
using System.Windows.Data;

namespace RiotAutoLogin.Converters
{
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 4 || values[0] == null || values[1] == null || values[2] == null || values[3] == null)
                return 0.0;

            if (double.TryParse(values[0].ToString(), out double value) &&
                double.TryParse(values[1].ToString(), out double minimum) &&
                double.TryParse(values[2].ToString(), out double maximum) &&
                double.TryParse(values[3].ToString(), out double actualWidth))
            {
                if (maximum <= minimum)
                    return 0.0;

                double percentage = (value - minimum) / (maximum - minimum);
                return Math.Max(0, Math.Min(actualWidth, actualWidth * percentage));
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 