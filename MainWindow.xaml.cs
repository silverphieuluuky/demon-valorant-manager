using RiotAutoLogin.Models;
using RiotAutoLogin.Services;
using RiotAutoLogin.Utilities;
using RiotAutoLogin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
// System.Text.Json removed - not needed for hotkey settings
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms; // Required for NotifyIcon
using Path = System.IO.Path;
using Microsoft.Extensions.Logging;
using System.Windows.Threading;

namespace RiotAutoLogin
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            try
            {
                // Initialize logging first
                LoggingService.Initialize();
                _logger.LogInformation("Initializing MainWindow");

                InitializeComponent();
                
                // Initialize ViewModel
                _viewModel = new MainViewModel(_logger);
                DataContext = _viewModel;

                // Services initialization removed - not needed
                
                // Setup event handlers
                this.Closing += MainWindow_Closing;
                this.KeyDown += MainWindow_KeyDown;

                _logger.LogInformation("MainWindow initialized successfully");
            }
            catch (Exception ex)
            {
                LoggingService.LogError(ex, "Failed to initialize MainWindow");
                throw;
            }
        }

        // DllImports to bring a window to the front.
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
        
        // Cursor position and screen detection removed - not needed

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Monitor info structures removed - not needed

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
        private const int SW_MAXIMIZE = 3;

        // Data and configuration fields.
        private List<Account> _accounts = new List<Account>();
        private readonly string _configFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RiotClientAutoLogin", "accounts.json");

        // System Tray Icon
        private NotifyIcon? _notifyIcon;

        // Hotkey functionality removed - not needed

        // Update Service
        // private UpdateService? _updateService; // Removed - user doesn't want update functionality

        private string _selectedAvatarPath = string.Empty;
        private Dictionary<Account, Border> _accountCardMap = new Dictionary<Account, Border>();
        // Quick login popup functionality removed - not needed
        private readonly ILogger _logger = LoggingService.GetLogger<MainWindow>();
        private bool _isLoginInProgress = false;

        // Progress animation fields
        private double _currentProgress = 0;
        private double _targetProgress = 0;
        private DispatcherTimer? _progressTimer;

        // Constructor cũ đã được thay thế bằng constructor mới ở trên

        

        // Quick login popup initialization removed - not needed
            
        private void RefreshUI()
        {
            RefreshAccountLists();
            UpdateTotalGameStats();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new NotifyIcon();

            try
            {
                // Try to load the custom icon from embedded resources first
                var resourceStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/resources/9rur41socqn71.ico"));
                if (resourceStream != null)
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(resourceStream.Stream);
                    _logger.LogDebug("Custom system tray icon loaded from resources");
                }
                else
                {
                    // Try to load from file path as fallback
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "9rur41socqn71.ico");
                    if (File.Exists(iconPath))
                    {
                        _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                        _logger.LogDebug("Custom system tray icon loaded from file path");
                    }
                    else
                    {
                        _logger.LogDebug("Custom icon not found, using system icon");
                        // Fallback to system icon
                        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading custom system tray icon");
                try
                {
                    // Attempt to load a generic system icon as a fallback
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                    _logger.LogDebug("Fallback system tray icon loaded");
                }
                catch (Exception exSysIcon)
                {
                    _logger.LogError(exSysIcon, "Error loading fallback system tray icon");
                    // If even this fails, the tray icon likely won't show, but the app might still run.
                }
            }
            
            _notifyIcon.Visible = false; // Initially hidden, will be shown on minimize or X-close
            _notifyIcon.Text = "Riot Auto Login";

            // Handle double-click to show the main window
            _notifyIcon.DoubleClick += (s, args) => ShowMainWindow();

            // Context menu for the tray icon
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("Show", null, (s, args) => ShowMainWindow());
            _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, args) => ExitApplication());
        }

        

        // InitializeUpdateService and related update methods removed - user doesn't want update functionality

        // btnCheckUpdates_Click method removed - user doesn't want update check functionality

        // Hotkey and quick login popup functionality removed - not needed




        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            SetForegroundWindow(new System.Windows.Interop.WindowInteropHelper(this).Handle); // Bring to front
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        private void ExitApplication()
        {
            try
            {
                _logger.LogInformation("Application shutting down - cleaning up resources");
                
                // Clean up system tray icon
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose(); 
                    _notifyIcon = null;
                }
                
                // Force close all background tasks and processes
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // Force application shutdown
                System.Windows.Application.Current.Shutdown();
                
                // If shutdown doesn't work, force exit
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application shutdown");
                // Force exit even if there's an error
                Environment.Exit(1);
            }
        }
        
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                // Only minimize to system tray if the setting is enabled
                if (_viewModel?.SettingsViewModel?.MinimizeToTrayEnabled == true)
                {
                    this.Hide();
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Visible = true;
                        _notifyIcon.ShowBalloonTip(2000, "Demon Valorant Manager", "Application minimized to system tray", ToolTipIcon.Info);
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("Application starting - Loading accounts and initializing");
            
            // Set window position to center of screen for better drag experience
            try
            {
                var screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
                var screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
                var windowWidth = this.Width;
                var windowHeight = this.Height;
                
                this.Left = (screenWidth - windowWidth) / 2;
                this.Top = (screenHeight - windowHeight) / 2;
                
                _logger.LogDebug("Window positioned at center: Left={Left}, Top={Top}", this.Left, this.Top);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting window position");
            }
            
            // Initialize system tray
            InitializeNotifyIcon();
            
            // Load settings
            LoadSettings();
            
            // Load accounts FIRST before doing anything else
            LoadAccounts();
            
            // Load accounts into ViewModels
            _viewModel.LoadAccountsCommand.Execute(null);
            UpdateTotalGameStats();
            
            // Load and display saved rank data AFTER accounts are loaded
            LoadSavedRankData();

            // Load API keys and update accounts in background - REMOVED to prevent background processes
            // Background tasks removed to prevent application running in background

            // Set default tab to LOGIN
            try
            {
                SetDefaultTab();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default tab");
                // Continue with application startup even if tab setting fails
            }
            
            // Update current version display
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version ?? new Version("1.0.0");
                // txtCurrentVersion.Text = $"Current version: v{version.ToString(3)}"; // Only show major.minor.patch
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating version display");
                // txtCurrentVersion.Text = "Current version: v1.0.0";
            }
            
            // Update check on startup removed - user doesn't want update functionality
            
            _logger.LogInformation("Application fully loaded and ready to use");
        }

        #region Window Control Event Handlers

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Allow drag from title bar, window background, and specific drag areas
                if (e.Source == this || 
                    e.Source is Border || 
                    e.Source is Grid ||
                    e.Source is TextBlock ||
                    sender is Border) // This handles the title bar Border
                {
                    // Don't capture mouse - let WPF handle it naturally
                    // This prevents drag issues
                    
                    // Perform drag operation
                    DragMove();
                    
                    e.Handled = true;
                    
                    _logger.LogDebug("Window dragged to: Left={Left}, Top={Top}", this.Left, this.Top);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during window drag: {Message}", ex.Message);
                // Don't handle the exception - let it bubble up
            }
        }

        private void btnSelectAvatar_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _selectedAvatarPath = openFileDialog.FileName;
                UpdateAvatarPreview(_selectedAvatarPath);
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            // Only minimize to system tray if the setting is enabled
            if (_viewModel?.SettingsViewModel?.MinimizeToTrayEnabled == true)
            {
                this.Hide(); // Hide the window
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = true; // Show system tray icon
                    _notifyIcon.ShowBalloonTip(2000, "Demon Valorant Manager", "Application minimized to system tray", ToolTipIcon.Info);
                }
            }
            else
            {
                // Normal minimize behavior
                this.WindowState = WindowState.Minimized;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Close button clicked - shutting down application");
                
                // Force close all background tasks
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // Exit application completely
                ExitApplication();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during close button click");
                // Force exit even if there's an error
                Environment.Exit(1);
            }
        }

        // btnToggleTheme_Click method removed - user doesn't want theme toggle functionality

        #endregion

        #region Manage Accounts Event Handlers

        // Remove all code-behind event handlers and methods related to the old Manage tab controls: btnAddAccount_Click, btnUpdateAccount_Click, btnDeleteAccount_Click, lbAccounts_SelectionChanged, and any code referencing txtGameName, txtTagLine, txtUsername, txtPassword, lbAccounts, or similar. Also remove any unused variables such as 'ex'.

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                // Update ViewModel when password changes
                if (_viewModel?.ManageViewModel != null)
                {
                    _viewModel.ManageViewModel.Password = txtPassword.Password;
                    _logger.LogDebug($"Password updated in ViewModel, length: {txtPassword.Password.Length}");
                }
                else
                {
                    _logger.LogWarning("ViewModel or ManageViewModel is null - cannot update password");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password in ViewModel");
            }
        }

        /// <summary>
        /// Sync password from ViewModel to UI (called when form is cleared)
        /// </summary>
        private void SyncPasswordToUI()
        {
            try
            {
                if (_viewModel?.ManageViewModel != null)
                {
                    // Clear password field in UI
                    txtPassword.Password = string.Empty;
                    _logger.LogDebug("Password field cleared in UI");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing password to UI");
            }
        }

        private void UpdateAvatarPreview(string path)
        {
            // Avatar preview functionality removed - not implemented in current UI
        }

        private void RefreshAccountLists()
        {
            try
            {
                // Load accounts into ViewModels
                _viewModel.LoadAccountsCommand.Execute(null);
                
                // Synchronize local _accounts list with ViewModels
                if (_viewModel?.Accounts != null)
                {
                    _accounts.Clear();
                    foreach (var account in _viewModel.Accounts)
                    {
                        _accounts.Add(account);
                    }
                    
                    _logger.LogDebug($"Refreshed account lists and synchronized {_accounts.Count} accounts");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing account lists");
            }
        }

        private void LoadAccounts()
        {
            try
            {
                _accounts = AccountService.LoadAccounts();
                
                // Also update ViewModels if they exist
                if (_viewModel?.Accounts != null)
                {
                    _viewModel.Accounts.Clear();
                    foreach (var account in _accounts)
                    {
                        _viewModel.Accounts.Add(account);
                    }
                    
                    // Update other ViewModels as well
                    if (_viewModel.LoginViewModel?.LoginAccounts != null)
                    {
                        _viewModel.LoginViewModel.LoginAccounts.Clear();
                        foreach (var account in _accounts)
                        {
                            _viewModel.LoginViewModel.LoginAccounts.Add(account);
                        }
                    }
                    
                    if (_viewModel.ManageViewModel?.Accounts != null)
                    {
                        _viewModel.ManageViewModel.Accounts.Clear();
                        foreach (var account in _accounts)
                        {
                            _viewModel.ManageViewModel.Accounts.Add(account);
                        }
                    }
                    
                    _logger.LogDebug($"Loaded {_accounts.Count} accounts and synchronized with all ViewModels");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading accounts");
            }
        }

        private void LoadSettings()
        {
            try
            {
                // Load settings from ViewModel
                if (_viewModel?.SettingsViewModel != null)
                {
                    // Settings will be loaded automatically by SettingsViewModel constructor
                    _logger.LogDebug("Settings loaded successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings");
            }
        }

        private void LoadSavedRankData()
        {
            try
            {
                // Load accounts with saved rank data
                var accounts = AccountService.LoadAccounts();
                
                // Update ViewModels with saved rank data
                if (_viewModel != null)
                {
                    foreach (var account in accounts)
                    {
                        // Find and update the account in ViewModels
                        var existingAccount = _viewModel.Accounts.FirstOrDefault(a => 
                            a.GameName == account.GameName && a.TagLine == account.TagLine);
                        
                        if (existingAccount != null)
                        {
                            // Update rank data
                            existingAccount.CurrentRank = account.CurrentRank;
                            existingAccount.PeakRank = account.PeakRank;
                            existingAccount.RankRating = account.RankRating;
                            existingAccount.LastRankUpdate = account.LastRankUpdate;
                            existingAccount.IsRankLoaded = account.IsRankLoaded;
                            existingAccount.IsRankFailed = account.IsRankFailed;
                            existingAccount.LastError = account.LastError;
                        }
                    }
                    
                    // ALSO update the local _accounts list to keep it in sync
                    foreach (var account in accounts)
                    {
                        var existingLocalAccount = _accounts.FirstOrDefault(a => 
                            a.GameName == account.GameName && a.TagLine == account.TagLine);
                        
                        if (existingLocalAccount != null)
                        {
                            // Update rank data in local list
                            existingLocalAccount.CurrentRank = account.CurrentRank;
                            existingLocalAccount.PeakRank = account.PeakRank;
                            existingLocalAccount.RankRating = account.RankRating;
                            existingLocalAccount.LastRankUpdate = account.LastRankUpdate;
                            existingLocalAccount.IsRankLoaded = account.IsRankLoaded;
                            existingLocalAccount.IsRankFailed = account.IsRankFailed;
                            existingLocalAccount.LastError = account.LastError;
                        }
                    }
                    
                    _logger.LogDebug($"Loaded saved rank data for {accounts.Count} accounts and synchronized with local list");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading saved rank data");
            }
        }

        private void SaveAccounts()
        {
            try
            {
                // Ensure we have the latest rank data from ViewModels before saving
                if (_viewModel?.Accounts != null)
                {
                    foreach (var viewModelAccount in _viewModel.Accounts)
                    {
                        var localAccount = _accounts.FirstOrDefault(a => 
                            a.GameName == viewModelAccount.GameName && a.TagLine == viewModelAccount.TagLine);
                        
                        if (localAccount != null)
                        {
                            // Sync rank data from ViewModel to local list
                            localAccount.CurrentRank = viewModelAccount.CurrentRank;
                            localAccount.PeakRank = viewModelAccount.PeakRank;
                            localAccount.RankRating = viewModelAccount.RankRating;
                            localAccount.LastRankUpdate = viewModelAccount.LastRankUpdate;
                            localAccount.IsRankLoaded = viewModelAccount.IsRankLoaded;
                            localAccount.IsRankFailed = viewModelAccount.IsRankFailed;
                            localAccount.LastError = viewModelAccount.LastError;
                        }
                    }
                }
                
                // Save the synchronized accounts
                var success = AccountService.SaveAccounts(_accounts);
                if (success)
                {
                    _logger.LogDebug($"Successfully saved {_accounts.Count} accounts with rank data");
                }
                else
                {
                    _logger.LogWarning("Failed to save accounts");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving accounts");
            }
        }

        #endregion

        #region Login Tab Event Handlers

        private void AccountCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // This method only handles MANAGE ACCOUNTS cards now
            if (sender is Border clickedBorder && clickedBorder.Tag is Account account)
            {
                _logger.LogDebug("Manage account card clicked: {GameName}", account.GameName);
                // lbAccounts.SelectedItem = account; // Removed
            }
        }

        private void AccountItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.DataContext is Account account)
            {
                _logger.LogDebug("Account item clicked: {GameName}", account.GameName);
                
                // Fill the form with account data
                if (_viewModel?.ManageViewModel != null)
                {
                    _viewModel.ManageViewModel.EditAccount(account);
                }
                
                // Add animation effect
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 0.8,
                    Duration = TimeSpan.FromMilliseconds(100),
                    AutoReverse = true
                };
                
                clickedBorder.BeginAnimation(OpacityProperty, animation);
            }
        }

        // NEW: Completely rewritten - no async, no await, pure simplicity
        private void LoginCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag is Account account)
            {
                _logger.LogDebug("LOGIN card clicked: {GameName}", account.GameName);
                
                // Prevent multiple clicks
                if (_isLoginInProgress)
                {
                    _logger.LogDebug("Login already in progress, ignoring click");
                    return;
                }
                
                // Start login process - REMOVED background thread to prevent background processes
                // StartLoginProcess(account); // Disabled to prevent background execution
            }
        }

        private void StartLoginProcess(Account account)
        {
            try
            {
                _isLoginInProgress = true;
                _logger.LogInformation("Starting background login for: {GameName}", account.GameName);
                
                // Update UI status
                Dispatcher.Invoke(() => UpdateStatusText($"Logging in as {account.GameName}..."));
                
                // Focus window first
                FocusRiotClientWindow();
                
                // Small delay
                Thread.Sleep(500);
                
                // Decrypt password
                string password = EncryptionService.DecryptString(account.EncryptedPassword);
                if (string.IsNullOrEmpty(password))
                {
                    _logger.LogError("Failed to decrypt password for account: {GameName}", account.GameName);
                    Dispatcher.Invoke(() => UpdateStatusText("Login failed - password error"));
                    return;
                }
                
                // Call the automation service (this is already async internally)
                var riotService = new RiotClientAutomationService(LoggingService.GetLogger<RiotClientAutomationService>());
                var loginTask = riotService.LaunchAndLoginAsync(account.AccountName, password);
                loginTask.Wait(); // Wait for completion
                
                _logger.LogInformation("Login process completed for: {GameName}", account.GameName);
                Dispatcher.Invoke(() => UpdateStatusText("Login completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in login process for account: {GameName}", account.GameName);
                Dispatcher.Invoke(() => UpdateStatusText("Login failed"));
            }
            finally
            {
                _isLoginInProgress = false;
            }
        }

        private void lbLoginAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This method is intentionally empty - login is handled by LoginCard_MouseDown
        }

        private void lbLoginAccounts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lbLoginAccounts.SelectedItem is Account account)
            {
                _logger.LogDebug("Double-click login for: {GameName}", account.GameName);
                // Task.Run(() => StartLoginProcess(account)); // Disabled to prevent background execution
            }
        }

        private void btnVerifyAccounts_Click(object sender, RoutedEventArgs e)
        {
            // Verification feature removed - user doesn't want verification
            System.Windows.MessageBox.Show("Account verification feature has been removed as requested.", "Feature Removed", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void btnFetchRanks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowLoadingIndicator(true);
                UpdateStatusText("Fetching ranks...");
                
                // Bắt đầu hiệu ứng loading ống nước
                var progressTask = AnimateProgressBarAsync(4000, "Fetching ranks..."); // 4 giây cho fetch ranks
                
                foreach (var account in _accounts)
                {
                    await AccountService.UpdateAccountRanksAsync(account);
                }
                
                RefreshAccountLists();
                SaveAccounts();
                
                // Đợi hiệu ứng loading xong
                await progressTask;
                
                ShowLoadingIndicator(false);
                UpdateStatusText("Rank fetching complete");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching ranks");
                ShowLoadingIndicator(false);
                UpdateStatusText("Rank fetching failed");
                System.Windows.MessageBox.Show($"Error fetching ranks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnQuickFetchRank_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_accounts == null || _accounts.Count == 0)
                {
                    System.Windows.MessageBox.Show("No accounts found. Please add some accounts first.", "No Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Disable button during scan
                // btnQuickFetchRank.IsEnabled = false;
                UpdateProgressStatus("Starting scan...");

                // Start water wave animation
                StartWaterWaveAnimation(0);

                var henrikDevService = new HenrikDevService();
                var successfulFetches = 0;
                var failedFetches = 0;
                var results = new List<string>();

                for (int i = 0; i < _accounts.Count; i++)
                {
                    var account = _accounts[i];
                    UpdateProgressStatus($"Scanning {account.GameName}... ({i + 1}/{_accounts.Count})");

                    try
                    {
                        var profile = await henrikDevService.GetPlayerProfileAsync(
                            account.GameName, 
                            account.TagLine, 
                            !string.IsNullOrEmpty(account.Region) ? account.Region : ServerSettingsService.GetDefaultServer(), 
                            ApiKeyManager.GetHenrikDevApiKey(),
                            maxRetries: 3
                        );
                        if (profile != null)
                        {
                            account.CurrentRank = profile.DisplayRank;
                            account.RankRating = profile.RankRating;
                            account.LastRankUpdate = DateTime.Now;
                            account.LastError = string.Empty; // Clear error
                            
                            // Check if rank is valid (not unrated/unranked/unknown)
                            var rank = profile.CurrentRank.Trim().ToLower();
                            if (rank != "unrated" && rank != "unranked" && rank != "unknown")
                            {
                                account.IsRankLoaded = true;
                                account.IsRankFailed = false;
                                successfulFetches++;
                                results.Add($"✅ {account.GameName}: {profile.DisplayRank} ({profile.RankRating})");
                            }
                            else
                            {
                                // Valid response but no actual rank
                                account.IsRankLoaded = false;
                                account.IsRankFailed = false;
                                account.LastError = string.Empty;
                                successfulFetches++;
                                results.Add($"⚠️ {account.GameName}: {profile.DisplayRank} (No actual rank)");
                            }
                        }
                        else
                        {
                            account.LastError = "Failed to fetch rank data";
                            failedFetches++;
                            results.Add($"❌ {account.GameName}: No data found");
                        }
                    }
                    catch (Exception ex)
                    {
                        account.LastError = $"Error: {ex.Message}";
                        failedFetches++;
                        results.Add($"❌ {account.GameName}: Error - {ex.Message}");
                        _logger.LogError(ex, $"Error fetching rank for {account.GameName}");
                    }

                    // Update water wave progress smoothly
                    double progress = (i + 1) / (double)_accounts.Count;
                    StartWaterWaveAnimation(progress);

                    // Longer delay for smoother animation
                    await Task.Delay(1000); // 1 second delay for natural feel
                }

                // Ensure 100% completion
                StartWaterWaveAnimation(1.0);
                await Task.Delay(500); // Wait for final animation

                // Save updated accounts
                SaveAccounts();
                RefreshAccountLists();

                // Stop water wave animation
                StopWaterWaveAnimation();

                // Show results
                var resultMessage = $"Scan completed!\n\nSuccessful: {successfulFetches}\nFailed: {failedFetches}\n\nResults:\n{string.Join("\n", results)}";
                // System.Windows.MessageBox.Show(resultMessage, "Scan All Profiles Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Update progress bar to show completion with neon flicker effect
                UpdateProgressStatus($"Scan completed! Success: {successfulFetches}, Failed: {failedFetches}");
                StartNeonFlickerEffect();
                UpdateStatusText($"Scan completed: {successfulFetches} successful, {failedFetches} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scan all profiles");
                UpdateProgressStatus("Scan failed!");
                UpdateStatusText("Scan failed");
                System.Windows.MessageBox.Show($"Error during scan: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // btnQuickFetchRank.IsEnabled = true;
            }
        }

        private async void btnFetchAllRanks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_accounts == null || _accounts.Count == 0)
                {
                    System.Windows.MessageBox.Show("No accounts found. Please add some accounts first.", "No Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirm with user
                var result = System.Windows.MessageBox.Show(
                    $"This will fetch ranks for all {_accounts.Count} saved profiles.\n\nThis may take a while and will show progress for each account.\n\nContinue?",
                    "Fetch All Ranks",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                ShowLoadingIndicator(true);
                UpdateStatusText($"Starting to fetch ranks for {_accounts.Count} accounts...");

                // Log the start of rank fetch operation
                RankFetchLogger.LogRankFetchStart("Fetch All Ranks", _accounts.Count);

                // Bắt đầu hiệu ứng loading ống nước
                var progressTask = AnimateProgressBarAsync(5000, "Starting rank scan..."); // 5 giây cho fetch all ranks

                var henrikDevService = new HenrikDevService();
                var successfulFetches = 0;
                var failedFetches = 0;
                var noDataFetches = 0;
                var results = new List<string>();

                for (int i = 0; i < _accounts.Count; i++)
                {
                    var account = _accounts[i];
                    UpdateStatusText($"Fetching rank {i + 1}/{_accounts.Count}: {account.GameName}#{account.TagLine}");
                    UpdateProgressStatus($"Scanning {i + 1}/{_accounts.Count}: {account.GameName}");

                    // Set loading state for this account
                    account.IsRankLoading = true;
                    account.LastError = string.Empty; // Clear previous errors
                    
                    // Force UI update
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    {
                        // Trigger property change notification
                        if (_viewModel?.LoginViewModel?.LoginAccounts != null)
                        {
                            var index = _viewModel.LoginViewModel.LoginAccounts.IndexOf(account);
                            if (index >= 0)
                            {
                                _viewModel.LoginViewModel.LoginAccounts[index] = account;
                            }
                        }
                    });

                    try
                    {
                        var profile = await henrikDevService.GetPlayerProfileAsync(
                            account.GameName, 
                            account.TagLine, 
                            !string.IsNullOrEmpty(account.Region) ? account.Region : ServerSettingsService.GetDefaultServer(), 
                            ApiKeyManager.GetHenrikDevApiKey(),
                            maxRetries: 3
                        );
                        
                        if (profile != null)
                        {
                            // Update the account with new rank data
                            account.CurrentRank = profile.CurrentRank;
                            account.RankRating = profile.RankRating;
                            account.LastRankUpdate = DateTime.UtcNow;
                            account.LastError = string.Empty; // Clear error
                            account.IsRankLoading = false; // Stop loading

                            // Check if rank is valid (not unrated/unranked/unknown)
                            var rank = profile.CurrentRank.Trim().ToLower();
                            if (rank != "unrated" && rank != "unranked" && rank != "unknown")
                            {
                                account.IsRankLoaded = true;
                                account.IsRankFailed = false;
                                successfulFetches++;
                                results.Add($"✅ {account.GameName}#{account.TagLine}: {profile.DisplayRank} (Rating: {profile.RankRating})");
                            }
                            else
                            {
                                // Valid response but no actual rank
                                account.IsRankLoaded = false;
                                account.IsRankFailed = false;
                                account.LastError = string.Empty;
                                noDataFetches++;
                                results.Add($"⚠️ {account.GameName}#{account.TagLine}: {profile.DisplayRank} (No actual rank)");
                            }
                        }
                        else
                        {
                            account.LastError = "Failed to fetch rank data";
                            account.IsRankLoading = false; // Stop loading
                            failedFetches++;
                            results.Add($"❌ {account.GameName}#{account.TagLine}: No rank data found");
                        }
                    }
                    catch (Exception ex)
                    {
                        account.LastError = $"Error: {ex.Message}";
                        account.IsRankLoading = false; // Stop loading
                        failedFetches++;
                        results.Add($"❌ {account.GameName}#{account.TagLine}: Error - {ex.Message}");
                        _logger.LogError(ex, $"Error fetching rank for {account.GameName}#{account.TagLine}");
                    }

                    // Force UI update after each account
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                    {
                        if (_viewModel?.LoginViewModel?.LoginAccounts != null)
                        {
                            var index = _viewModel.LoginViewModel.LoginAccounts.IndexOf(account);
                            if (index >= 0)
                            {
                                _viewModel.LoginViewModel.LoginAccounts[index] = account;
                            }
                        }
                    });

                    // Small delay between requests to avoid rate limiting
                    await Task.Delay(1000);
                }

                // Log the summary
                RankFetchLogger.LogRankFetchSummary(_accounts.Count, successfulFetches, failedFetches, noDataFetches);

                // Save updated accounts
                SaveAccounts();

                // Show results
                var summary = $"Rank Fetch Complete!\n\n" +
                             $"✅ Successful: {successfulFetches}\n" +
                             $"❌ Failed: {failedFetches}\n" +
                             $"⚠️ No Data: {noDataFetches}\n" +
                             $"📊 Total: {_accounts.Count}\n\n" +
                             $"Detailed Results:\n" +
                             string.Join("\n", results);

                System.Windows.MessageBox.Show(summary, "Fetch All Ranks - Complete", MessageBoxButton.OK, 
                    successfulFetches > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

                UpdateStatusText($"Completed: {successfulFetches} successful, {failedFetches} failed, {noDataFetches} no data");
                ShowLoadingIndicator(false);

                // Refresh the UI to show updated ranks
                RefreshAccountLists();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fetch all ranks");
                ShowLoadingIndicator(false);
                UpdateStatusText("Fetch all ranks failed");
                System.Windows.MessageBox.Show($"Error fetching all ranks: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnUpdateAgent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is Account account)
                {
                    // Populate the form with the selected account
                    // txtUsername.Text = account.AccountName; // Removed as per edit hint
                    // txtGameName.Text = account.GameName; // Removed as per edit hint
                    // txtTagLine.Text = account.TagLine; // Removed as per edit hint
                    // Region is now handled directly from account.Region
                    
                    // Update region buttons
                    // Region selection logic removed - buttons don't exist in XAML
                    
                    // Update avatar preview
                    if (!string.IsNullOrEmpty(account.AvatarPath))
                    {
                        UpdateAvatarPreview(account.AvatarPath);
                    }
                    
                    // Switch to Account Management tab
                    // TabControl.SelectedIndex = 1; // Account Management tab - Removed as TabControl no longer exists
                    
                    UpdateStatusText($"Selected {account.GameName} for update");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account");
                System.Windows.MessageBox.Show($"Error updating account: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteAgent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is Account account)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Are you sure you want to delete the account '{account.GameName}'?\n\nThis action cannot be undone.",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        _accounts.Remove(account);
                        RefreshAccountLists();
                        SaveAccounts();
                        UpdateTotalGameStats();
                        
                        UpdateStatusText($"Deleted account {account.GameName}");
                        System.Windows.MessageBox.Show($"Account '{account.GameName}' has been deleted.", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account");
                System.Windows.MessageBox.Show($"Error deleting account: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Update All Accounts

        private async Task UpdateAllAccountsAsync()
        {
            await AccountService.UpdateAllAccountsAsync(_accounts);
        }

        private void UpdateTotalGameStats()
        {
            // Update stats from ViewModel
            _viewModel.AccountCount = _viewModel.Accounts.Count;
            _viewModel.LastUpdated = DateTime.Now;
            UpdateStatusText("Ready");
        }
        
        private void UpdateStatusText(string status)
        {
            // Status text functionality removed - not implemented in current UI
        }
        
        private void ShowLoadingIndicator(bool show)
        {
            // Loading indicator functionality removed - not implemented in current UI
        }

        #endregion

        #region Riot Client Automation and Settings

        #region Sidebar Tab Event Handlers

        private void LoginTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SwitchToTab("LOGIN", "Quick Login", "Select an account to login quickly", LoginTabContent);
                SetActiveTab(LoginTab, ManageTab, SettingsTab);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching to LOGIN tab");
            }
        }

        private void ManageTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SwitchToTab("MANAGE", "Account Management", "Manage your Valorant accounts and information", ManageTabContent);
                SetActiveTab(ManageTab, LoginTab, SettingsTab);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching to MANAGE tab");
            }
        }

        private void SettingsTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SwitchToTab("SETTINGS", "Settings", "Configure application settings and preferences", SettingsTabContent);
                SetActiveTab(SettingsTab, LoginTab, ManageTab);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error switching to SETTINGS tab");
            }
        }

        private void SwitchToTab(string tabName, string title, string subtitle, UIElement contentToShow)
        {
            // Hide all tab content first
            LoginTabContent.Visibility = Visibility.Collapsed;
            ManageTabContent.Visibility = Visibility.Collapsed;
            SettingsTabContent.Visibility = Visibility.Collapsed;

            // Show selected content
            contentToShow.Visibility = Visibility.Visible;

            // Update header
            txtContentTitle.Text = title;
            txtContentSubtitle.Text = subtitle;

            // Load API key for settings tab if needed
            if (tabName == "SETTINGS")
            {
                try
                {
                    var apiKeyTextBox = VisualTreeHelperExtensions.FindVisualChildren<System.Windows.Controls.TextBox>(this)
                        .FirstOrDefault(tb => tb.Name == "txtHenrikDevApiKey");
                    if (apiKeyTextBox != null)
                    {
                        string apiKey = ApiKeyManager.GetApiKey();
                        if (!string.IsNullOrEmpty(apiKey))
                        {
                            apiKeyTextBox.Text = "••••••••••••••••••••" + apiKey.Substring(Math.Max(0, apiKey.Length - 4));
                            apiKeyTextBox.ToolTip = "API key is saved. Enter a new key to update.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading API key for display");
                }
            }
        }

        private void SetActiveTab(Border activeTab, params Border[] inactiveTabs)
        {
            // Reset all tabs to inactive state
            ResetTabStyles(LoginTab, ManageTab, SettingsTab);

            // Set active tab style using the predefined style
            activeTab.Style = FindResource("ActiveSidebarTabStyle") as Style;

            // Create a new TranslateTransform for animation (avoid frozen transforms from style)
            var transform = new TranslateTransform(0, 0);
            activeTab.RenderTransform = transform;

            // Animate active tab slide to right
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 5,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            
            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void ResetTabStyles(params Border[] tabs)
        {
            foreach (var tab in tabs)
            {
                // Reset to smooth sidebar tab style
                tab.Style = FindResource("SmoothSidebarTabStyle") as Style;
                
                // Create a new TranslateTransform for animation (avoid frozen transforms from style)
                var transform = new TranslateTransform(5, 0);
                tab.RenderTransform = transform;
                
                // Reset transform with animation
                var animation = new DoubleAnimation
                {
                    From = 5,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(200)
                };
                
                transform.BeginAnimation(TranslateTransform.XProperty, animation);
            }
        }

        #endregion

        private void SetDefaultTab()
        {
            // Set LOGIN tab as default active tab
            SetActiveTab(LoginTab, ManageTab, SettingsTab);
            
            // Show LOGIN content by default
            SwitchToTab("LOGIN", "Quick Login", "Select an account to login quickly", LoginTabContent);
            
            // Ensure all tabs start with smooth style
            LoginTab.Style = FindResource("SmoothSidebarTabStyle") as Style;
            ManageTab.Style = FindResource("SmoothSidebarTabStyle") as Style;
            SettingsTab.Style = FindResource("SmoothSidebarTabStyle") as Style;
        }

        private void btnSaveApiKey_Click(object sender, RoutedEventArgs e)
        {
            var apiKeyTextBox = VisualTreeHelperExtensions.FindVisualChildren<System.Windows.Controls.TextBox>(this)
                .FirstOrDefault(tb => tb.Name == "txtApiKey");
            if (apiKeyTextBox == null)
            {
                System.Windows.MessageBox.Show("Cannot find API key text box.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string apiKey = apiKeyTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                System.Windows.MessageBox.Show("Please enter a valid API key.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            bool success = ApiKeyManager.SaveApiKey(apiKey);
            if (success)
            {
                apiKeyTextBox.Text = "••••••••••••••••••••" + apiKey.Substring(Math.Max(0, apiKey.Length - 4));
                System.Windows.MessageBox.Show("Riot API key saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to save API key. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnSaveHenrikDevApiKey_Click(object sender, RoutedEventArgs e)
        {
            var apiKeyTextBox = VisualTreeHelperExtensions.FindVisualChildren<System.Windows.Controls.TextBox>(this)
                .FirstOrDefault(tb => tb.Name == "txtHenrikDevApiKey");
            if (apiKeyTextBox == null)
            {
                System.Windows.MessageBox.Show("Cannot find HenrikDev API key text box.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string apiKey = apiKeyTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                System.Windows.MessageBox.Show("Please enter a valid HenrikDev API key.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            bool success = ApiKeyManager.SaveHenrikDevApiKey(apiKey);
            if (success)
            {
                apiKeyTextBox.Text = "••••••••••••••••••••" + apiKey.Substring(Math.Max(0, apiKey.Length - 4));
                System.Windows.MessageBox.Show("HenrikDev API key saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to save HenrikDev API key. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // APAC region is now default - no need for region selection



        protected override void OnClosed(EventArgs e)
        {
            // This method is called when the window is truly closing (e.g., after Application.Shutdown() is called).
            // Ensure resources are released here.
            if (_notifyIcon != null)
            {
                _notifyIcon.Dispose();
            }
            
            base.OnClosed(e);
        }



        #endregion

        #region (Optional) Helper Methods

        // Example: Cache account card borders for quick access (if your XAML uses an ItemsControl named icAccounts).
        // Remove the CacheAccountCards method and any code that references it, as well as any code that uses _accountCardMap or icAccounts. These are obsolete in the new MVVM UI.

        #endregion

        private void StartWaterWaveAnimation(double targetProgress)
        {
            _targetProgress = targetProgress;
            _logger.LogDebug("Starting water wave animation to: {Progress}", targetProgress);
            
            // Start smooth progress animation
            StartSmoothProgressAnimation();
            
            // Wave animation removed - not needed for current UI
        }

        private void StartSmoothProgressAnimation()
        {
            var waveCanvas = FindName("waveCanvas") as Canvas;
            if (waveCanvas == null) return;

            var container = waveCanvas.Parent as Grid;
            if (container == null) return;

            double containerWidth = container.ActualWidth;
            double fromWidth = _currentProgress * containerWidth;
            double toWidth = _targetProgress * containerWidth;

            // Create smooth width animation
            var widthAnimation = new DoubleAnimation
            {
                From = fromWidth,
                To = toWidth,
                Duration = TimeSpan.FromMilliseconds(1500), // Slower for more natural feel
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            // Create storyboard
            var storyboard = new Storyboard();
            storyboard.Children.Add(widthAnimation);

            Storyboard.SetTarget(widthAnimation, waveCanvas);
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(Canvas.WidthProperty));

            // Start animation
            storyboard.Begin();
            
            // Update current progress manually during animation
            storyboard.Completed += (s, e) => 
            {
                _currentProgress = _targetProgress;
            };

            // Start progress timer to update _currentProgress smoothly
            if (_progressTimer == null)
            {
                _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // 60 FPS
                _progressTimer.Tick += (s, e) => UpdateProgressSmoothly();
            }
            _progressTimer.Start();
        }

        private void UpdateProgressSmoothly()
        {
            if (Math.Abs(_currentProgress - _targetProgress) < 0.005) // More precise
            {
                _currentProgress = _targetProgress;
                if (_progressTimer != null)
                {
                    _progressTimer.Stop();
                }
                return;
            }

            // Smoother progress interpolation
            double step = (_targetProgress - _currentProgress) * 0.03; // Slower interpolation
            _currentProgress += step;
        }

        private void UpdateWaves()
        {
            // Wave animation removed - not needed for current UI
        }

        private void DrawNaturalWave(System.Windows.Shapes.Path wavePath, double width, double height, double baseY, double phase, double frequency, double amplitude, int points)
        {
            var geo = new PathGeometry();
            var fig = new PathFigure { StartPoint = new Point(0, height) };

            // Create more natural wave with multiple harmonics
            for (int i = 0; i <= points; i++)
            {
                double x = i * width / points;
                double waveY = baseY;
                
                // Primary wave
                waveY += Math.Sin(x * frequency / width + phase) * amplitude;
                
                // Secondary harmonics for more natural look
                waveY += Math.Sin(x * frequency * 2 / width + phase * 1.5) * amplitude * 0.4;
                waveY += Math.Sin(x * frequency * 3 / width + phase * 2.1) * amplitude * 0.2;
                
                // Add some randomness for natural feel
                waveY += Math.Sin(x * frequency * 0.5 / width + phase * 0.7) * amplitude * 0.1;
                
                fig.Segments.Add(new LineSegment(new Point(x, waveY), true));
            }

            fig.Segments.Add(new LineSegment(new Point(width, height), true));
            fig.IsClosed = true;
            geo.Figures.Add(fig);
            wavePath.Data = geo;
        }

        private void StopWaterWaveAnimation()
        {
            _logger.LogDebug("Stopping water wave animation");
            if (_progressTimer != null)
            {
                _progressTimer.Stop();
            }

            // Smooth reset animation
            var waveCanvas = FindName("waveCanvas") as Canvas;
            if (waveCanvas != null)
            {
                var resetAnimation = new DoubleAnimation
                {
                    From = waveCanvas.Width,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(2500), // Slower reset
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                waveCanvas.BeginAnimation(Canvas.WidthProperty, resetAnimation);
            }

            // Reset all values
            _currentProgress = 0;
            _targetProgress = 0;
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _logger.LogInformation("MainWindow_Closing: Application is shutting down completely");
                
                // Don't cancel the closing - let it close completely
                e.Cancel = false;
                
                // Clean up system tray icon
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
                
                // Force close all background tasks and processes
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // Force application shutdown
                System.Windows.Application.Current.Shutdown();
                
                // If shutdown doesn't work, force exit
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MainWindow_Closing");
                // Force exit even if there's an error
                Environment.Exit(1);
            }
        }

        // Hotkey settings functionality removed - not needed

        // Hotkey and startup functionality removed - not needed

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Enable Enter key for quick login when an account is selected
            if (e.Key == Key.Enter && lbLoginAccounts?.SelectedItem is Account account)
            {
                _logger.LogDebug("Enter key pressed for login: {GameName}", account.GameName);
                
                // Show Riot Client window if it exists
                Process[] processes = Process.GetProcessesByName("Riot Client");
                if (processes.Length > 0)
                {
                    IntPtr hWnd = processes[0].MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                        SetForegroundWindow(hWnd);
                    }
                }
                
                // Perform login
                string decryptedPassword = EncryptionService.DecryptString(account.EncryptedPassword);
                if (!string.IsNullOrEmpty(decryptedPassword))
                {
                    var riotService = new RiotClientAutomationService(LoggingService.GetLogger<RiotClientAutomationService>());
                    _ = riotService.LaunchAndLoginAsync(account.AccountName, decryptedPassword);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to decrypt password for the selected account.", 
                        "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                e.Handled = true;
            }
        }

        private static void FocusRiotClientWindow()
        {
            try
            {
                // Try different process names that Riot Client might use
                string[] processNames = { "Riot Client", "RiotClientServices", "RiotClientUx" };
                Process? riotProcess = null;
                
                foreach (string processName in processNames)
                {
                    Process[] processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                    {
                        // Find the process with a visible main window
                        foreach (Process proc in processes)
                        {
                            if (proc.MainWindowHandle != IntPtr.Zero)
                            {
                                riotProcess = proc;
                                break;
                            }
                        }
                        if (riotProcess != null) break;
                    }
                }
                
                if (riotProcess == null)
                {
                    return;
                }
                
                IntPtr hWnd = riotProcess.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                {
                    return;
                }
                
                // Multi-step approach to ensure window gets focus
                
                // Step 1: Restore if minimized
                if (IsIconic(hWnd))
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    Thread.Sleep(200); // Short delay to allow restore
                }
                
                // Step 2: Show the window
                ShowWindow(hWnd, SW_SHOW);
                Thread.Sleep(100);
                
                // Step 3: Bring to top
                BringWindowToTop(hWnd);
                Thread.Sleep(100);
                
                // Step 4: Force foreground (with thread attachment trick)
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow != hWnd)
                {
                    uint currentThreadId = GetCurrentThreadId();
                    uint targetThreadId = GetWindowThreadProcessId(hWnd, out uint targetProcessId);
                    
                    if (targetThreadId != currentThreadId)
                    {
                        AttachThreadInput(currentThreadId, targetThreadId, true);
                        SetForegroundWindow(hWnd);
                        AttachThreadInput(currentThreadId, targetThreadId, false);
                    }
                    else
                    {
                        SetForegroundWindow(hWnd);
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail - window focus is not critical for functionality
            }
        }

        private async void PerformAccountLogin(Account account)
        {
            if (_isLoginInProgress)
            {
                _logger.LogDebug("Login already in progress, skipping");
                return;
            }

            _isLoginInProgress = true;
            try
            {
                _logger.LogInformation("Starting login for: {GameName}", account.GameName);
                
                // Focus the Riot Client window using the robust method
                FocusRiotClientWindow();
                
                // Small delay to ensure window is ready for automation
                await Task.Delay(300);
                
                var riotService = new RiotClientAutomationService(LoggingService.GetLogger<RiotClientAutomationService>());
                await riotService.LaunchAndLoginAsync(
                    account.AccountName,
                    EncryptionService.DecryptString(account.EncryptedPassword));
            }
            finally
            {
                _isLoginInProgress = false;
            }
        }

        // Mouse screen center functionality removed - not needed

        private async Task AnimateProgressBarAsync(int durationMs = 2000, string statusText = "Processing...")
        {
            // if (progressScanRank == null) return;
            
            // Cập nhật text status
            UpdateProgressStatus(statusText);
            
                            // progressScanRank.Value = 0;
            int steps = 100;
            int delay = durationMs / steps;
            
            for (int i = 0; i <= steps; i++)
            {
                                    // progressScanRank.Value = i;
                await Task.Delay(delay);
            }
            
            // Không ẩn ProgressBar nữa, để luôn hiển thị
            UpdateProgressStatus("Ready to scan...");
        }

        private void UpdateProgressStatus(string status)
        {
            if (txtProgressStatus != null)
            {
                txtProgressStatus.Text = status;
            }
        }

        private async Task AnimateWaterProgressAsync(int durationMs = 3000, string statusText = "Processing...")
        {
            try
            {
                // Update progress text
                UpdateProgressStatus(statusText);

                // Get the progress bar elements
                var progressBarFill = this.FindName("progressBarFill") as Rectangle;
                var waterRipple = this.FindName("waterRipple") as Rectangle;
                var progressTextBlock = this.FindName("progressText") as TextBlock;

                if (progressBarFill != null && waterRipple != null)
                {
                    // Reset progress
                    progressBarFill.Width = 0;
                    waterRipple.Width = 0;

                    // Get the container width for percentage calculation
                    var container = progressBarFill.Parent as Grid;
                    if (container != null)
                    {
                        // Force layout update to get actual width
                        container.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        container.Arrange(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                        
                        var containerWidth = container.ActualWidth;
                        if (containerWidth <= 0) containerWidth = 400; // Fallback width

                        // Animate progress bar fill
                        var fillAnimation = new DoubleAnimation
                        {
                            From = 0,
                            To = containerWidth,
                            Duration = TimeSpan.FromMilliseconds(durationMs),
                            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseInOut, Exponent = 2 }
                        };

                        // Animate water ripple with slight delay
                        var rippleAnimation = new DoubleAnimation
                        {
                            From = 0,
                            To = containerWidth,
                            Duration = TimeSpan.FromMilliseconds(durationMs + 200),
                            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseInOut, Exponent = 2 }
                        };

                        progressBarFill.BeginAnimation(Rectangle.WidthProperty, fillAnimation);
                        waterRipple.BeginAnimation(Rectangle.WidthProperty, rippleAnimation);

                        // Wait for animation to complete
                        await Task.Delay(durationMs + 500);

                        // Reset
                        progressBarFill.BeginAnimation(Rectangle.WidthProperty, null);
                        waterRipple.BeginAnimation(Rectangle.WidthProperty, null);
                        progressBarFill.Width = 0;
                        waterRipple.Width = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in water progress animation");
            }
        }

        private void AnimateWaterProgressAsync()
        {
            var progressBarFill = FindName("progressBarFill") as Rectangle;
            var waterRipple = FindName("waterRipple") as Rectangle;
            
            if (progressBarFill != null && waterRipple != null)
            {
                var container = progressBarFill.Parent as Grid;
                if (container != null)
                {
                    var containerWidth = container.ActualWidth;
                    
                    // Reset to cyan neon color
                    progressBarFill.Fill = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop(Color.FromRgb(0, 255, 255), 0.0),
                            new GradientStop(Color.FromArgb(26, 0, 255, 255), 1.0)
                        }
                    };
                    
                    // Smooth animation with better easing
                    var fillAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = containerWidth,
                        Duration = TimeSpan.FromSeconds(2),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };
                    
                    var rippleAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = containerWidth,
                        Duration = TimeSpan.FromSeconds(2.5),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                    };
                    
                    progressBarFill.BeginAnimation(Rectangle.WidthProperty, fillAnimation);
                    waterRipple.BeginAnimation(Rectangle.WidthProperty, rippleAnimation);
                }
            }
        }

        private void StartNeonFlickerEffect()
        {
            var progressBarFill = FindName("progressBarFill") as Rectangle;
            var progressText = FindName("progressText") as TextBlock;
            
            if (progressBarFill != null && progressText != null)
            {
                // Create neon flicker animation
                var flickerAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.3,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseInOut, Exponent = 2 },
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };
                
                var textFlickerAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.5,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseInOut, Exponent = 2 },
                    RepeatBehavior = RepeatBehavior.Forever,
                    AutoReverse = true
                };
                
                progressBarFill.BeginAnimation(UIElement.OpacityProperty, flickerAnimation);
                progressText.BeginAnimation(UIElement.OpacityProperty, textFlickerAnimation);
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Check if scroll is possible
                if (scrollViewer.ScrollableHeight > 0)
                {
                    // Calculate scroll amount - increase speed by 3x
                    double scrollAmount = e.Delta > 0 ? -60 : 60; // Negative for up, positive for down
                    
                    // Apply smooth scrolling with increased speed
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + scrollAmount);
                    
                    e.Handled = true;
                }
                else
                {
                    // If this ScrollViewer can't scroll, bubble up to parent
                    e.Handled = false;
                }
            }
        }

        private void cmbSortRank_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var sortType = selectedItem.Tag?.ToString() ?? "None";
                _viewModel.LoginViewModel.ApplySortCommand.Execute(sortType);
            }
        }

        // Hiện thông báo nhỏ (snackbar/toast) ở góc dưới
        public async void ShowSnackbar(string message, int durationMs = 2000)
        {
            if (Snackbar != null && SnackbarText != null)
            {
                SnackbarText.Text = message;
                Snackbar.Visibility = Visibility.Visible;
                await Task.Delay(durationMs);
                Snackbar.Visibility = Visibility.Collapsed;
            }
        }

        // Hotkey functionality removed - not needed
    }
}