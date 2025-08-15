using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using RiotAutoLogin.Models;
using RiotAutoLogin.ViewModels;
using Microsoft.Extensions.Logging;

namespace RiotAutoLogin.Services
{
    /// <summary>
    /// Handles UI events and interactions for the main window
    /// </summary>
    public class UIEventHandlers
    {
        private readonly ILogger _logger;
        private readonly MainViewModel _viewModel;

        public UIEventHandlers(MainViewModel viewModel, ILogger logger)
        {
            _viewModel = viewModel;
            _logger = logger;
        }

        /// <summary>
        /// Handles window mouse left button down for dragging
        /// </summary>
        public void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (sender is Window window)
                    {
                        window.DragMove();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling window drag");
            }
        }

        /// <summary>
        /// Handles minimize button click
        /// </summary>
        public void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Parent is FrameworkElement parent)
                {
                    if (parent.Parent is Window window)
                    {
                        window.WindowState = WindowState.Minimized;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error minimizing window");
            }
        }

        /// <summary>
        /// Handles close button click
        /// </summary>
        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Parent is FrameworkElement parent)
                {
                    if (parent.Parent is Window window)
                    {
                        window.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing window");
            }
        }

        /// <summary>
        /// Handles password text changed event
        /// </summary>
        public void Password_TextChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is PasswordBox passwordBox)
                {
                    // Update the password in the current account
                    if (_viewModel.CurrentAccount != null)
                    {
                        // Note: In a real implementation, you'd want to encrypt this
                        _viewModel.CurrentAccount.EncryptedPassword = passwordBox.Password;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling password change");
            }
        }

        /// <summary>
        /// Handles account card mouse down for selection
        /// </summary>
        public void AccountCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement element && element.DataContext is Account account)
                {
                    _viewModel.SelectAccount(account);
                    
                    // Add visual feedback
                    if (element is Border border)
                    {
                        var animation = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.95,
                            Duration = TimeSpan.FromMilliseconds(100),
                            AutoReverse = true
                        };
                        border.BeginAnimation(UIElement.OpacityProperty, animation);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling account card selection");
            }
        }

        /// <summary>
        /// Handles login card mouse down for login process
        /// </summary>
        public void LoginCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is FrameworkElement element && element.DataContext is Account account)
                {
                    _viewModel.StartLoginProcess();
                    
                    // Add visual feedback
                    if (element is Border border)
                    {
                        var animation = new DoubleAnimation
                        {
                            From = 1.0,
                            To = 0.8,
                            Duration = TimeSpan.FromMilliseconds(200),
                            AutoReverse = true,
                            RepeatBehavior = RepeatBehavior.Forever
                        };
                        border.BeginAnimation(UIElement.OpacityProperty, animation);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling login card click");
            }
        }

        /// <summary>
        /// Handles tab control selection changed
        /// </summary>
        public void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
                {
                    _viewModel.UpdateContentBasedOnTab();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling tab selection change");
            }
        }

        /// <summary>
        /// Handles key down events for global shortcuts
        /// </summary>
        public void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Handle global shortcuts
                if (e.Key == Key.F1)
                {
                    // Show help
                    _viewModel.ShowHelp();
                }
                else if (e.Key == Key.F5)
                {
                    // Refresh ranks
                    _viewModel.RefreshAllRanks();
                }
                else if (e.Key == Key.F12)
                {
                    // Toggle developer mode
                    _viewModel.ToggleDeveloperMode();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling key down event");
            }
        }
    }
} 