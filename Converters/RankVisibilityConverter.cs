using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RiotAutoLogin.Converters
{
    public class RankVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Nếu parameter là UnrankedOnly: chỉ hiện khi rank là Unranked/Unrated/Unknown
            if (parameter?.ToString() == "UnrankedOnly")
            {
                string rankStr = string.Empty;
                if (value is RiotAutoLogin.Models.Account acc)
                    rankStr = acc.DisplayRank?.ToLower() ?? string.Empty;
                else if (value is string s)
                    rankStr = s.ToLower();
                if (rankStr == "unranked" || rankStr == "unrated" || rankStr == "unknown" || string.IsNullOrEmpty(rankStr))
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
            // Nếu parameter là RankedOnly: chỉ hiện khi rank là rank thật
            if (parameter?.ToString() == "RankedOnly")
            {
                string rankStr = string.Empty;
                if (value is RiotAutoLogin.Models.Account acc)
                    rankStr = acc.DisplayRank?.ToLower() ?? string.Empty;
                else if (value is string s)
                    rankStr = s.ToLower();
                if (rankStr != "unranked" && rankStr != "unrated" && rankStr != "unknown" && !string.IsNullOrEmpty(rankStr))
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
            // Mặc định: chỉ hiện icon với rank thật
            if (value is string rankStr2)
            {
                if (!string.IsNullOrEmpty(rankStr2) && rankStr2 != "unranked" && rankStr2 != "unrated" && rankStr2 != "unknown")
                    return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 