using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using RiotAutoLogin.Utilities;

namespace RiotAutoLogin.Services
{
    public static class UIService
    {
        public static void ApplyTheme(Window window, bool isDarkMode)
        {
            var (mainBg, cardBg, secondaryBg, textColor, statsBg, winColor, lossColor, cardBorder) = GetThemeColors(isDarkMode);

            // Update resource dictionaries
            window.Resources["MainBackgroundBrush"] = mainBg;
            window.Resources["CardBackgroundBrush"] = cardBg;
            window.Resources["SecondaryBackgroundBrush"] = secondaryBg;
            window.Resources["TextColorBrush"] = textColor;

            // Update window background
            window.Background = isDarkMode ? 
                new SolidColorBrush(Color.FromRgb(10, 10, 16)) : 
                new SolidColorBrush(Color.FromRgb(245, 245, 250));

            // Update stats panel
            var statsPanel = window.FindName("statsPanel") as Border;
            if (statsPanel != null)
                statsPanel.Background = statsBg;

            // Update main content border
            var mainContentBorder = VisualTreeHelperExtensions.FindVisualChildren<Border>(window)
                .FirstOrDefault(b => b.CornerRadius.TopLeft == 20);
            if (mainContentBorder != null)
                mainContentBorder.Background = mainBg;

            UpdateTabHeaders(window, isDarkMode);
            UpdateCardBackgrounds(window, cardBg, cardBorder, textColor, winColor, lossColor);
        }

        private static (SolidColorBrush main, SolidColorBrush card, SolidColorBrush secondary, 
                       SolidColorBrush text, SolidColorBrush stats, SolidColorBrush win, 
                       SolidColorBrush loss, SolidColorBrush border) GetThemeColors(bool isDarkMode)
        {
            if (isDarkMode)
            {
                return (
                    new SolidColorBrush(Color.FromRgb(18, 18, 24)),
                    new SolidColorBrush(Color.FromRgb(26, 26, 36)),
                    new SolidColorBrush(Color.FromRgb(46, 46, 58)),
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(34, 34, 34)),
                    new SolidColorBrush(Color.FromRgb(34, 187, 187)),
                    new SolidColorBrush(Color.FromRgb(242, 68, 5)),
                    new SolidColorBrush(Color.FromRgb(40, 40, 50))
                );
            }
            else
            {
                return (
                    new SolidColorBrush(Colors.White),
                    new SolidColorBrush(Color.FromRgb(235, 235, 240)),
                    new SolidColorBrush(Color.FromRgb(220, 220, 230)),
                    new SolidColorBrush(Colors.Black),
                    new SolidColorBrush(Color.FromRgb(225, 225, 225)),
                    new SolidColorBrush(Color.FromRgb(0, 128, 128)),
                    new SolidColorBrush(Color.FromRgb(180, 0, 0)),
                    new SolidColorBrush(Color.FromRgb(200, 200, 210))
                );
            }
        }

        private static void UpdateTabHeaders(Window window, bool isDarkMode)
        {
            var selectedTabBrush = isDarkMode ?
                new SolidColorBrush(Color.FromRgb(42, 42, 54)) :
                new SolidColorBrush(Color.FromRgb(220, 220, 230));

            var normalTabBrush = isDarkMode ?
                new SolidColorBrush(Color.FromRgb(26, 26, 36)) :
                new SolidColorBrush(Color.FromRgb(235, 235, 240));

            foreach (var tabItem in VisualTreeHelperExtensions.FindVisualChildren<TabItem>(window))
            {
                var tabHeader = VisualTreeHelperExtensions.FindVisualChildren<Border>(tabItem)
                    .FirstOrDefault(b => b.Padding.Top == 10 && b.CornerRadius.TopLeft == 10);
                
                if (tabHeader != null)
                {
                    if (tabItem.IsSelected)
                    {
                        tabHeader.Background = selectedTabBrush;
                        tabHeader.BorderThickness = new Thickness(0, 0, 0, 2);
                        tabHeader.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 82, 82));
                    }
                    else
                    {
                        tabHeader.Background = normalTabBrush;
                        tabHeader.BorderThickness = new Thickness(0);
                    }
                }
            }
        }

        private static void UpdateCardBackgrounds(Window window, SolidColorBrush cardBg, SolidColorBrush cardBorder,
            SolidColorBrush textColor, SolidColorBrush winColor, SolidColorBrush lossColor)
        {
            // Remove all references to icAccounts, including field declarations and usages.
        }

        private static void UpdateTextBlockColors(Border border, SolidColorBrush textColor, 
            SolidColorBrush winColor, SolidColorBrush lossColor)
        {
            foreach (var textBlock in VisualTreeHelperExtensions.FindVisualChildren<TextBlock>(border))
            {
                if (textBlock.Inlines.Count == 0)
                {
                    textBlock.Foreground = textColor;
                }
                else
                {
                    foreach (var inline in textBlock.Inlines.OfType<Run>())
                    {
                        inline.Foreground = textColor;
                    }
                }
            }
        }

        public static void UpdateTotalGameStats(Window window, System.Collections.Generic.List<Models.Account> accounts)
        {
            var txtTotalGames = window.FindName("txtTotalGames") as TextBlock;
            if (txtTotalGames != null)
            {
                txtTotalGames.Text = $"Total Accounts: {accounts?.Count ?? 0}";
            }
        }
    }
} 