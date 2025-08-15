using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media;
using RiotAutoLogin.Models;

namespace RiotAutoLogin.Controls
{
    /// <summary>
    /// Enhanced account card control with animations and interactions
    /// </summary>
    public partial class AccountCard : UserControl
    {
        private Storyboard? _hoverAnimation;
        private Storyboard? _clickAnimation;
        private Storyboard? _loadingAnimation;
        private bool _isSelected = false;

        public event EventHandler<Account>? LoginRequested;
        public event EventHandler<Account>? RefreshRankRequested;
        public event EventHandler<Account>? AccountSelected;

        public AccountCard()
        {
            InitializeComponent();
            InitializeAnimations();
        }

        /// <summary>
        /// Initializes the animations for the card
        /// </summary>
        private void InitializeAnimations()
        {
            _hoverAnimation = (Storyboard)FindResource("HoverAnimation");
            _clickAnimation = (Storyboard)FindResource("ClickAnimation");
            _loadingAnimation = (Storyboard)FindResource("LoadingAnimation");
        }

        /// <summary>
        /// Handles mouse enter event for hover animation
        /// </summary>
        private void CardBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                _hoverAnimation?.Begin();
                
                // Add neon glow effect
                var glowEffect = CardBorder.Effect as DropShadowEffect;
                if (glowEffect != null)
                {
                    var animation = new DoubleAnimation
                    {
                        From = 0,
                        To = 0.8,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };
                    glowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, animation);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in hover animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles mouse leave event to reset hover animation
        /// </summary>
        private void CardBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            try
            {
                _hoverAnimation?.Stop();
                
                // Reset scale to normal
                var scaleTransform = CardBorder.RenderTransform as ScaleTransform;
                if (scaleTransform != null)
                {
                    scaleTransform.ScaleX = 1.0;
                    scaleTransform.ScaleY = 1.0;
                }
                
                // Remove neon glow effect
                var glowEffect = CardBorder.Effect as DropShadowEffect;
                if (glowEffect != null)
                {
                    var animation = new DoubleAnimation
                    {
                        From = 0.8,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(200)
                    };
                    glowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, animation);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting hover animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles mouse left button down for click animation and selection
        /// </summary>
        private void CardBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Play click animation
                _clickAnimation?.Begin();
                
                // Select the account
                if (DataContext is Account account)
                {
                    SelectAccount(account);
                    AccountSelected?.Invoke(this, account);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in click animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles login button click
        /// </summary>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is Account account)
                {
                    LoginRequested?.Invoke(this, account);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling login request: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles refresh rank button click
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is Account account)
                {
                    RefreshRankRequested?.Invoke(this, account);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling refresh rank request: {ex.Message}");
            }
        }

        /// <summary>
        /// Selects the account and shows visual feedback
        /// </summary>
        public void SelectAccount(Account account)
        {
            try
            {
                _isSelected = true;
                SelectionIndicator.Visibility = Visibility.Visible;
                
                // Add selection animation
                var animation = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                SelectionIndicator.BeginAnimation(UIElement.OpacityProperty, animation);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting account: {ex.Message}");
            }
        }

        /// <summary>
        /// Deselects the account and hides visual feedback
        /// </summary>
        public void DeselectAccount()
        {
            try
            {
                _isSelected = false;
                
                // Add deselection animation
                var animation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                animation.Completed += (s, e) => SelectionIndicator.Visibility = Visibility.Collapsed;
                SelectionIndicator.BeginAnimation(UIElement.OpacityProperty, animation);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deselecting account: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts the loading animation for rank fetching
        /// </summary>
        public void StartRankLoading()
        {
            try
            {
                _loadingAnimation?.Begin();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting rank loading animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the loading animation
        /// </summary>
        public void StopRankLoading()
        {
            try
            {
                _loadingAnimation?.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping rank loading animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets whether the account is currently selected
        /// </summary>
        public bool IsSelected => _isSelected;

        /// <summary>
        /// Updates the card's visual state based on the account's rank loading state
        /// </summary>
        public void UpdateRankLoadingState()
        {
            try
            {
                if (DataContext is Account account)
                {
                    if (account.IsRankLoading)
                    {
                        StartRankLoading();
                    }
                    else
                    {
                        StopRankLoading();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating rank loading state: {ex.Message}");
            }
        }
    }
} 