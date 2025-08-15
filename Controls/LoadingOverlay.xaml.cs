using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RiotAutoLogin.Controls
{
    /// <summary>
    /// Loading overlay control for providing user feedback during operations
    /// </summary>
    public partial class LoadingOverlay : UserControl
    {
        private Storyboard? _loadingAnimation;
        private Storyboard? _pulseAnimation;
        private bool _isVisible = false;

        public event EventHandler? CancelRequested;

        public LoadingOverlay()
        {
            InitializeComponent();
            InitializeAnimations();
        }

        /// <summary>
        /// Initializes the loading animations
        /// </summary>
        private void InitializeAnimations()
        {
            _loadingAnimation = (Storyboard)FindResource("LoadingAnimation");
            _pulseAnimation = (Storyboard)FindResource("PulseAnimation");
        }

        /// <summary>
        /// Shows the loading overlay with the specified message
        /// </summary>
        public void Show(string message = "Loading...", bool showProgress = false, bool showCancel = false)
        {
            try
            {
                LoadingText.Text = message;
                ProgressBar.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
                ProgressText.Visibility = showProgress ? Visibility.Visible : Visibility.Collapsed;
                CancelButton.Visibility = showCancel ? Visibility.Visible : Visibility.Collapsed;

                OverlayGrid.Visibility = Visibility.Visible;
                _isVisible = true;

                // Start animations
                _loadingAnimation?.Begin();
                _pulseAnimation?.Begin();
            }
            catch (Exception ex)
            {
                // Log error if logger is available
                System.Diagnostics.Debug.WriteLine($"Error showing loading overlay: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the loading overlay
        /// </summary>
        public void Hide()
        {
            try
            {
                OverlayGrid.Visibility = Visibility.Collapsed;
                _isVisible = false;

                // Stop animations
                _loadingAnimation?.Stop();
                _pulseAnimation?.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding loading overlay: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the loading message
        /// </summary>
        public void UpdateMessage(string message)
        {
            try
            {
                LoadingText.Text = message;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating loading message: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the progress value (0-100)
        /// </summary>
        public void UpdateProgress(double progress)
        {
            try
            {
                progress = Math.Max(0, Math.Min(100, progress));
                ProgressBar.Value = progress;
                ProgressText.Text = $"{progress:F0}%";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets whether the overlay is currently visible
        /// </summary>
        public bool IsOverlayVisible => _isVisible;

        /// <summary>
        /// Handles the cancel button click
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CancelRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling cancel request: {ex.Message}");
            }
        }
    }
} 