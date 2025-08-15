using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RiotAutoLogin.Converters
{
    public class RankToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string rank)
            {
                rank = rank.ToLower();
                return rank switch
                {
                    var r when r.Contains("iron") => new SolidColorBrush(Color.FromRgb(139, 115, 85)), // #8B7355
                    var r when r.Contains("bronze") => new SolidColorBrush(Color.FromRgb(205, 127, 50)), // #CD7F32
                    var r when r.Contains("silver") => new SolidColorBrush(Color.FromRgb(192, 192, 192)), // #C0C0C0
                    var r when r.Contains("gold") => new SolidColorBrush(Color.FromRgb(0, 255, 255)), // #00FFFF (Neon Blue)
                    var r when r.Contains("platinum") => new SolidColorBrush(Color.FromRgb(229, 228, 226)), // #E5E4E2
                    var r when r.Contains("diamond") => new SolidColorBrush(Color.FromRgb(185, 242, 255)), // #B9F2FF
                    var r when r.Contains("ascendant") => new SolidColorBrush(Color.FromRgb(0, 255, 255)), // #00FFFF (Neon Blue)
                    var r when r.Contains("immortal") => new SolidColorBrush(Color.FromRgb(0, 255, 255)), // #00FFFF (Neon Blue)
                    var r when r.Contains("radiant") => new SolidColorBrush(Color.FromRgb(0, 255, 255)), // #00FFFF (Neon Blue)
                    _ => new SolidColorBrush(Color.FromRgb(0, 255, 255)) // Default Neon Blue
                };
            }
            return new SolidColorBrush(Color.FromRgb(0, 255, 255)); // Default Neon Blue
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 