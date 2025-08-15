using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;

namespace RiotAutoLogin.Converters
{
    public class RankToIconConverter : IValueConverter
    {
        // Sửa lại đường dẫn icon rank cho đúng cấp thư mục
        private static readonly string RankIconsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "ranks");

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string rank && !string.IsNullOrEmpty(rank))
            {
                var iconPath = GetRankIconPath(rank);
                
                // Debug: Log the path being checked
                System.Diagnostics.Debug.WriteLine($"Looking for rank image: {iconPath}");
                System.Diagnostics.Debug.WriteLine($"Directory exists: {Directory.Exists(RankIconsPath)}");
                
                if (iconPath != null && File.Exists(iconPath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                        bitmap.EndInit();
                        return bitmap;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading image {iconPath}: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Rank image not found: {iconPath}");
                    // KHÔNG fallback icon nào nữa, chỉ trả về null
                    return null;
                }
            }
            return null;
        }

        private string? GetRankIconPath(string rank)
        {
            if (string.IsNullOrWhiteSpace(rank))
                return null;
            rank = rank.ToLower().Trim();
            // Không trả về icon cho unranked/unrated/unknown hoặc các giá trị không hợp lệ
            if (rank == "unranked" || rank == "unrated" || rank == "unknown")
                return null;
            // Map rank names to file names with exact matching first
            var fileName = rank switch
            {
                // Iron ranks
                "iron 1" or "iron_1" => "Iron_1_Rank.png",
                "iron 2" or "iron_2" => "Iron_2_Rank.png",
                "iron 3" or "iron_3" => "Iron_3_Rank.png",
                
                // Bronze ranks
                "bronze 1" or "bronze_1" => "Bronze_1_Rank.png",
                "bronze 2" or "bronze_2" => "Bronze_2_Rank.png",
                "bronze 3" or "bronze_3" => "Bronze_3_Rank.png",
                
                // Silver ranks
                "silver 1" or "silver_1" => "Silver_1_Rank.png",
                "silver 2" or "silver_2" => "Silver_2_Rank.png",
                "silver 3" or "silver_3" => "Silver_3_Rank.png",
                
                // Gold ranks
                "gold 1" or "gold_1" => "Gold_1_Rank.png",
                "gold 2" or "gold_2" => "Gold_2_Rank.png",
                "gold 3" or "gold_3" => "Gold_3_Rank.png",
                
                // Platinum ranks
                "platinum 1" or "platinum_1" => "Platinum_1_Rank.png",
                "platinum 2" or "platinum_2" => "Platinum_2_Rank.png",
                "platinum 3" or "platinum_3" => "Platinum_3_Rank.png",
                
                // Diamond ranks
                "diamond 1" or "diamond_1" => "Diamond_1_Rank.png",
                "diamond 2" or "diamond_2" => "Diamond_2_Rank.png",
                "diamond 3" or "diamond_3" => "Diamond_3_Rank.png",
                
                // Ascendant ranks
                "ascendant 1" or "ascendant_1" => "Ascendant_1_Rank.png",
                "ascendant 2" or "ascendant_2" => "Ascendant_2_Rank.png",
                "ascendant 3" or "ascendant_3" => "Ascendant_3_Rank.png",
                
                // Immortal ranks
                "immortal 1" or "immortal_1" => "Immortal_1_Rank.png",
                "immortal 2" or "immortal_2" => "Immortal_2_Rank.png",
                "immortal 3" or "immortal_3" => "Immortal_3_Rank.png",
                
                // Radiant
                "radiant" => "Radiant_Rank.png",
                
                // Only exact matches for partial ranks (no fallback)
                var r when r.StartsWith("iron ") => "Iron_1_Rank.png",
                var r when r.StartsWith("bronze ") => "Bronze_1_Rank.png",
                var r when r.StartsWith("silver ") => "Silver_1_Rank.png",
                var r when r.StartsWith("gold ") => "Gold_1_Rank.png",
                var r when r.StartsWith("platinum ") => "Platinum_1_Rank.png",
                var r when r.StartsWith("diamond ") => "Diamond_1_Rank.png",
                var r when r.StartsWith("ascendant ") => "Ascendant_1_Rank.png",
                var r when r.StartsWith("immortal ") => "Immortal_1_Rank.png",
                var r when r.StartsWith("radiant") => "Radiant_Rank.png",
                
                _ => null // Return null for unknown ranks
            };
            
            return fileName != null ? Path.Combine(RankIconsPath, fileName) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter cho icon Error khi fetch rank lỗi
    public class RankErrorIconConverter : IValueConverter
    {
        private static readonly string RankIconsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "ranks");

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRankFailed && isRankFailed)
            {
                var errorIconPath = Path.Combine(RankIconsPath, "Error_Rank.png");
                
                if (File.Exists(errorIconPath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(errorIconPath, UriKind.Absolute);
                        bitmap.EndInit();
                        return bitmap;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading error icon: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Error icon not found: {errorIconPath}");
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 