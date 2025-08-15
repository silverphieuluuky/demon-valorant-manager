using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.Models;
using RiotAutoLogin.Constants;
using RiotAutoLogin.ViewModels;
using RiotAutoLogin.Services;
using RiotAutoLogin.Interfaces;
using System.Collections.Generic;
using System.Linq; // Added for FirstOrDefault
using System.Threading.Tasks;
using System.Windows;

namespace RiotAutoLogin.ViewModels
{
    public class ManageViewModel : BaseViewModel
    {
        private string _gameName = string.Empty;
        private string _tagLine = string.Empty;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _region = "AP"; // Changed default to AP
        private string _avatarPath = string.Empty;
        private string _defaultServer = "AP"; // New property for default server
        private Account? _selectedAccount;
        private bool _isEditing = false;
        private object? _selectedRegionItem; // For ComboBox binding

        public ObservableCollection<Account> Accounts { get; } = new ObservableCollection<Account>();

        // Reference to MainViewModel for synchronization
        private MainViewModel? _mainViewModel;

        private readonly IAccountService _accountService;

        public ManageViewModel(ILogger logger, IAccountService accountService) : base(logger)
        {
            _accountService = accountService;
            
            // Initialize commands
            AddAccountCommand = new RelayCommand(AddAccount);
            UpdateAccountCommand = new RelayCommand(UpdateAccount);
            DeleteAccountCommand = new RelayCommand(DeleteAccount);
            EditAccountCommand = new RelayCommand<Account>(EditAccount);
            SelectAvatarCommand = new RelayCommand(SelectAvatar);
            CancelEditCommand = new RelayCommand(CancelEdit);
            
            // New server management commands
            UpdateAllAccountsServerCommand = new AsyncRelayCommand(UpdateAllAccountsServer, () => !IsUpdating);
            TestServerConnectionCommand = new AsyncRelayCommand(TestServerConnection, () => !IsTesting);
            
            // Load default server from settings
            LoadDefaultServer();
        }

        // Method to set MainViewModel reference for synchronization
        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            try
            {
                LogInformation("Setting MainViewModel reference...");
                _mainViewModel = mainViewModel;
                
                // Synchronize accounts between tabs
                if (_mainViewModel != null)
                {
                    LogInformation("MainViewModel reference set successfully, synchronizing accounts...");
                    
                    // Use Dispatcher to ensure UI thread safety
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // Clear and reload accounts to ensure synchronization
                            Accounts.Clear();
                            foreach (var account in _mainViewModel.Accounts)
                            {
                                Accounts.Add(account);
                            }
                            
                            LogInformation($"Synchronized {Accounts.Count} accounts from MainViewModel");
                            
                            // Force immediate synchronization to ensure all tabs are in sync
                            ForceSynchronizeAccounts();
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, "Error during UI synchronization in SetMainViewModel");
                        }
                    });
                }
                else
                {
                    LogWarning("MainViewModel is null");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error setting MainViewModel reference");
            }
        }

        public string GameName
        {
            get => _gameName;
            set => SetProperty(ref _gameName, value);
        }

        public string TagLine
        {
            get => _tagLine;
            set => SetProperty(ref _tagLine, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Region
        {
            get => _region;
            set => SetProperty(ref _region, value);
        }

        public string AvatarPath
        {
            get => _avatarPath;
            set => SetProperty(ref _avatarPath, value);
        }

        public string DefaultServer
        {
            get => _defaultServer;
            set
            {
                if (SetProperty(ref _defaultServer, value))
                {
                    SaveDefaultServer();
                }
            }
        }

        public object? SelectedRegionItem
        {
            get => _selectedRegionItem;
            set
            {
                if (SetProperty(ref _selectedRegionItem, value))
                {
                    // Update Region property when SelectedRegionItem changes
                    if (value is System.Windows.Controls.ComboBoxItem comboBoxItem && comboBoxItem.Tag is string tag)
                    {
                        Region = tag;
                    }
                }
            }
        }

        public Account? SelectedAccount
        {
            get => _selectedAccount;
            set => SetProperty(ref _selectedAccount, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private bool _isUpdating = false;
        public bool IsUpdating
        {
            get => _isUpdating;
            set => SetProperty(ref _isUpdating, value);
        }

        private bool _isTesting = false;
        public bool IsTesting
        {
            get => _isTesting;
            set => SetProperty(ref _isTesting, value);
        }

        public ICommand AddAccountCommand { get; }
        public ICommand UpdateAccountCommand { get; }
        public ICommand DeleteAccountCommand { get; }
        public ICommand EditAccountCommand { get; }
        public ICommand SelectAvatarCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand UpdateAllAccountsServerCommand { get; }
        public ICommand TestServerConnectionCommand { get; }

        private void AddAccount()
        {
            try
            {
                LogInformation("Starting to add new account...");
                
                // Validate input fields
                if (string.IsNullOrWhiteSpace(GameName) || string.IsNullOrWhiteSpace(TagLine))
                {
                    LogWarning("Game name and tag line are required");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    LogWarning("Username and password are required");
                    return;
                }

                LogInformation($"Validating account: {GameName}#{TagLine} with username {Username}");

                // Check for duplicate account - allow multiple accounts with same GameName#TagLine if different username
                var existingAccount = Accounts.FirstOrDefault(a => 
                    a.GameName.Equals(GameName, StringComparison.OrdinalIgnoreCase) && 
                    a.TagLine.Equals(TagLine, StringComparison.OrdinalIgnoreCase) &&
                    a.AccountName.Equals(Username, StringComparison.OrdinalIgnoreCase));
                
                if (existingAccount != null)
                {
                    LogWarning($"Account {GameName}#{TagLine} with username {Username} already exists");
                    return;
                }

                LogInformation("Creating new account object...");
                
                var account = new Account
                {
                    GameName = GameName,
                    TagLine = TagLine,
                    AccountName = Username,
                    EncryptedPassword = EncryptionService.EncryptString(Password),
                    Region = Region,
                    AvatarPath = AvatarPath,
                    IsRankLoaded = false,
                    IsRankFailed = false,
                    IsRankLoading = false
                };

                LogInformation("Adding account to ManageViewModel collection...");
                
                // Add to ManageViewModel collection
                Accounts.Add(account);

                LogInformation($"Account added to ManageViewModel. Total accounts: {Accounts.Count}");

                // Synchronize with LoginViewModel if MainViewModel is available
                if (_mainViewModel != null)
                {
                    LogInformation("Synchronizing with MainViewModel and LoginViewModel...");
                    
                    // Use Dispatcher to ensure UI thread safety
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // Add to MainViewModel first
                            _mainViewModel.Accounts.Add(account);
                            _mainViewModel.AccountCount = _mainViewModel.Accounts.Count;
                            _mainViewModel.LastUpdated = DateTime.Now;
                            
                            // Add to LoginViewModel
                            _mainViewModel.LoginViewModel.LoginAccounts.Add(account);
                            
                            LogInformation($"Account synchronized with MainViewModel and LoginViewModel on UI thread");
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, "Error during UI synchronization");
                        }
                    });
                    
                    // Force synchronization between all tabs immediately
                    LogInformation("Forcing synchronization between all tabs...");
                    ForceSynchronizeAccounts();
                }
                else
                {
                    LogWarning("MainViewModel is null - cannot synchronize with other tabs");
                }

                LogInformation("Saving accounts to file...");
                
                // Save all accounts to file
                var allAccounts = new List<Account>();
                allAccounts.AddRange(Accounts);
                var saveResult = AccountService.SaveAccounts(allAccounts);
                
                if (saveResult)
                {
                    LogInformation($"Successfully saved {allAccounts.Count} accounts to file");
                }
                else
                {
                    LogWarning("Failed to save accounts to file");
                }

                LogInformation("Clearing form...");
                
                // Force clear all fields including password
                ClearForm();
                
                // Double-check password is cleared
                if (!string.IsNullOrEmpty(Password))
                {
                    LogWarning("Password field not cleared properly, forcing clear...");
                    Password = string.Empty;
                }
                
                LogInformation($"Successfully added account: {account.GameName}#{account.TagLine} with username {Username}");
                LogInformation("Form cleared and ready for next account");
            }
            catch (Exception ex)
            {
                LogError(ex, $"Error adding account: {ex.Message}");
                LogError(ex, $"Stack trace: {ex.StackTrace}");
            }
        }

        private void UpdateAccount()
        {
            try
            {
                if (SelectedAccount == null)
                {
                    LogWarning("No account selected for update");
                    return;
                }

                if (string.IsNullOrWhiteSpace(GameName) || string.IsNullOrWhiteSpace(TagLine))
                {
                    LogWarning("Game name and tag line are required");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Username))
                {
                    LogWarning("Username is required");
                    return;
                }

                // Check for duplicate account (excluding the current selected account)
                var existingAccount = Accounts.FirstOrDefault(a => 
                    a != SelectedAccount &&
                    a.GameName.Equals(GameName, StringComparison.OrdinalIgnoreCase) && 
                    a.TagLine.Equals(TagLine, StringComparison.OrdinalIgnoreCase));
                
                if (existingAccount != null)
                {
                    LogWarning($"Account {GameName}#{TagLine} already exists");
                    return;
                }

                // Update the account properties
                SelectedAccount.GameName = GameName;
                SelectedAccount.TagLine = TagLine;
                SelectedAccount.AccountName = Username;
                if (!string.IsNullOrWhiteSpace(Password))
                {
                    SelectedAccount.EncryptedPassword = EncryptionService.EncryptString(Password);
                }
                SelectedAccount.Region = Region;
                SelectedAccount.AvatarPath = AvatarPath;

                // Save all accounts to file
                var allAccounts = new List<Account>();
                allAccounts.AddRange(Accounts);
                AccountService.SaveAccounts(allAccounts);

                // Update MainViewModel
                if (_mainViewModel != null)
                {
                    _mainViewModel.LastUpdated = DateTime.Now;
                    
                    // Force synchronization between all tabs immediately
                    ForceSynchronizeAccounts();
                }

                ClearForm();
                LogInformation($"Updated account: {SelectedAccount.GameName}#{SelectedAccount.TagLine}");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error updating account");
            }
        }

        private void DeleteAccount()
        {
            try
            {
                LogInformation("Starting account deletion process...");
                
                if (SelectedAccount == null)
                {
                    LogWarning("No account selected for deletion");
                    return;
                }

                var accountName = $"{SelectedAccount.GameName}#{SelectedAccount.TagLine}";
                LogInformation($"Deleting account: {accountName}");
                
                // Store reference to the account being deleted
                var accountToDelete = SelectedAccount;
                
                // Remove from ManageViewModel collection
                LogInformation("Removing account from ManageViewModel collection...");
                var removeResult = Accounts.Remove(accountToDelete);
                LogInformation($"Account removal from ManageViewModel: {(removeResult ? "Success" : "Failed")}");

                // Synchronize with LoginViewModel if MainViewModel is available
                if (_mainViewModel != null)
                {
                    LogInformation("Synchronizing deletion with other ViewModels...");
                    
                    // Use Dispatcher to ensure UI thread safety
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // Remove from MainViewModel
                            var mainViewModelRemoveResult = _mainViewModel.Accounts.Remove(accountToDelete);
                            LogInformation($"Account removal from MainViewModel: {(mainViewModelRemoveResult ? "Success" : "Failed")}");
                            
                            // Remove from LoginViewModel
                            var loginViewModelRemoveResult = _mainViewModel.LoginViewModel.LoginAccounts.Remove(accountToDelete);
                            LogInformation($"Account removal from LoginViewModel: {(loginViewModelRemoveResult ? "Success" : "Failed")}");
                            
                            // Update account count and timestamp
                            _mainViewModel.AccountCount = _mainViewModel.Accounts.Count;
                            _mainViewModel.LastUpdated = DateTime.Now;
                            
                            LogInformation($"Account count updated to: {_mainViewModel.AccountCount}");
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, "Error during UI synchronization in DeleteAccount");
                        }
                    });
                    
                    // Force synchronization between all tabs immediately
                    LogInformation("Forcing synchronization between all tabs...");
                    ForceSynchronizeAccounts();
                }
                else
                {
                    LogWarning("MainViewModel is null - cannot synchronize deletion");
                }

                // Save all accounts to file
                LogInformation("Saving updated accounts to file...");
                var allAccounts = new List<Account>();
                allAccounts.AddRange(Accounts);
                var saveResult = AccountService.SaveAccounts(allAccounts);
                
                if (saveResult)
                {
                    LogInformation($"Successfully saved {allAccounts.Count} accounts to file after deletion");
                }
                else
                {
                    LogWarning("Failed to save accounts to file after deletion");
                }

                // Clear form and reset selection
                LogInformation("Clearing form and resetting selection...");
                ClearForm();
                SelectedAccount = null;
                
                LogInformation($"Successfully deleted account: {accountName}");
            }
            catch (Exception ex)
            {
                LogError(ex, $"Error deleting account: {ex.Message}");
                LogError(ex, $"Stack trace: {ex.StackTrace}");
            }
        }

        public void EditAccount(Account? account)
        {
            if (account == null) return;

            try
            {
                SelectedAccount = account;
                GameName = account.GameName;
                TagLine = account.TagLine;
                Username = account.AccountName;
                Region = account.Region;
                AvatarPath = account.AvatarPath;
                
                // Clear password field for security - user must re-enter if they want to change it
                Password = string.Empty;
                IsEditing = true;

                // Set the correct ComboBox item based on region
                SetSelectedRegionItem(account.Region);

                LogInformation($"Editing account: {account.GameName}#{account.TagLine}");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error editing account");
            }
        }

        public void SetSelectedRegionItem(string region)
        {
            // Find the ComboBoxItem that matches the region
            var regionItems = new[] { "AP", "NA", "EU", "KR", "BR", "LATAM" };
            var regionNames = new[] { "Asia Pacific (AP)", "North America (NA)", "Europe (EU)", "Korea (KR)", "Brazil (BR)", "Latin America (LATAM)" };
            
            for (int i = 0; i < regionItems.Length; i++)
            {
                if (regionItems[i] == region)
                {
                    // Create a ComboBoxItem with the correct content and tag
                    var comboBoxItem = new System.Windows.Controls.ComboBoxItem
                    {
                        Content = regionNames[i],
                        Tag = regionItems[i]
                    };
                    SelectedRegionItem = comboBoxItem;
                    break;
                }
            }
        }

        private void SelectAvatar()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Avatar Image",
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    AvatarPath = openFileDialog.FileName;
                    LogInformation($"Selected avatar: {AvatarPath}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error selecting avatar");
            }
        }

        private void CancelEdit()
        {
            try
            {
                ClearForm();
                LogInformation("Cancelled editing account");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error cancelling edit");
            }
        }

        private void ClearForm()
        {
            try
            {
                LogInformation("Starting to clear form...");
                
                // Clear all text fields
                GameName = string.Empty;
                TagLine = string.Empty;
                Username = string.Empty;
                Password = string.Empty;
                Region = "AP"; // Reset to default
                AvatarPath = string.Empty;
                
                // Reset form state
                SelectedAccount = null;
                IsEditing = false;
                
                // Reset region ComboBox selection
                SetSelectedRegionItem("AP");
                
                LogInformation("Form cleared successfully - all fields reset to default values");
                
                // Notify that form was cleared (for UI sync)
                OnPropertyChanged(nameof(Password));
            }
            catch (Exception ex)
            {
                LogError(ex, "Error clearing form");
            }
        }

        #region Server Management Methods

        private void LoadDefaultServer()
        {
            try
            {
                // Load default server from settings file
                var defaultServer = ServerSettingsService.GetDefaultServer();
                if (!string.IsNullOrEmpty(defaultServer))
                {
                    DefaultServer = defaultServer;
                }
                LogInformation($"Loaded default server: {DefaultServer}");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error loading default server");
            }
        }

        private void SaveDefaultServer()
        {
            try
            {
                ServerSettingsService.SaveDefaultServer(DefaultServer);
                LogInformation($"Saved default server: {DefaultServer}");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error saving default server");
            }
        }

        private async Task UpdateAllAccountsServer()
        {
            try
            {
                IsUpdating = true;
                LogInformation($"Updating all accounts to use server: {DefaultServer}");

                int updatedCount = 0;
                foreach (var account in Accounts)
                {
                    if (account.Region != DefaultServer)
                    {
                        account.Region = DefaultServer;
                        updatedCount++;
                    }
                }

                // Save all accounts to file
                var allAccounts = new List<Account>();
                allAccounts.AddRange(Accounts);
                await Task.Run(() => AccountService.SaveAccounts(allAccounts));

                // Update MainViewModel
                if (_mainViewModel != null)
                {
                    _mainViewModel.LastUpdated = DateTime.Now;
                }

                LogInformation($"Updated {updatedCount} accounts to use server: {DefaultServer}");
                
                MessageBox.Show(
                    $"Successfully updated {updatedCount} accounts to use server: {DefaultServer}",
                    "Server Update Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogError(ex, "Error updating all accounts server");
                MessageBox.Show(
                    $"Error updating accounts: {ex.Message}",
                    "Server Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsUpdating = false;
            }
        }

        private async Task TestServerConnection()
        {
            try
            {
                IsTesting = true;
                LogInformation($"Testing server connection for: {DefaultServer}");

                // Test with a sample account to verify server connectivity
                var henrikDevService = new HenrikDevService();
                var apiKey = ApiKeyManager.GetHenrikDevApiKey();

                if (string.IsNullOrEmpty(apiKey))
                {
                    MessageBox.Show(
                        "HenrikDev API key not configured. Please configure it in Settings first.",
                        "API Key Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Test with a known account (you can change this to a test account)
                var testProfile = await henrikDevService.GetPlayerProfileAsync(
                    "TestUser", 
                    "1234", 
                    DefaultServer, 
                    apiKey,
                    maxRetries: 1
                );

                if (testProfile != null)
                {
                    MessageBox.Show(
                        $"✅ Server connection successful!\n\nServer: {DefaultServer}\nStatus: Connected\nAPI Response: Valid",
                        "Server Test Result",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"⚠️ Server connection test completed.\n\nServer: {DefaultServer}\nStatus: No data returned\nNote: This might be normal if the test account doesn't exist.",
                        "Server Test Result",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                LogInformation($"Server connection test completed for: {DefaultServer}");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error testing server connection");
                MessageBox.Show(
                    $"❌ Server connection test failed:\n\nError: {ex.Message}\n\nPlease check your API key and internet connection.",
                    "Server Test Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsTesting = false;
            }
        }

                public void SynchronizeAccounts()
        {
            if (_mainViewModel == null) return;
            
            // Ensure all tabs have the same accounts
            var allAccounts = _mainViewModel.Accounts.ToList();
            
            // Update ManageViewModel
            Accounts.Clear();
            foreach (var account in allAccounts)
            {
                Accounts.Add(account);
            }
            
            // Update LoginViewModel
            _mainViewModel.LoginViewModel.LoginAccounts.Clear();
            foreach (var account in allAccounts)
            {
                _mainViewModel.LoginViewModel.LoginAccounts.Add(account);
            }
        }

        /// <summary>
        /// Force immediate synchronization between all tabs with proper error handling
        /// </summary>
        public void ForceSynchronizeAccounts()
        {
            try
            {
                if (_mainViewModel == null) return;
                
                LogInformation("Starting forced synchronization between tabs...");
                
                // Get the most up-to-date account list from ManageViewModel
                var currentAccounts = Accounts.ToList();
                
                LogInformation($"Synchronizing {currentAccounts.Count} accounts across all tabs...");
                
                // Use Dispatcher to ensure UI thread safety
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Instead of clearing and reloading, just ensure all collections have the same accounts
                        // This prevents UI flickering and binding issues
                        
                        // First, add any missing accounts to MainViewModel
                        foreach (var account in currentAccounts)
                        {
                            if (!_mainViewModel.Accounts.Any(a => 
                                a.GameName == account.GameName && 
                                a.TagLine == account.TagLine && 
                                a.AccountName == account.AccountName))
                            {
                                _mainViewModel.Accounts.Add(account);
                                LogInformation($"Added missing account to MainViewModel: {account.GameName}#{account.TagLine}");
                            }
                        }
                        
                        // Remove any extra accounts from MainViewModel
                        var accountsToRemove = _mainViewModel.Accounts.Where(a => 
                            !currentAccounts.Any(ca => 
                                ca.GameName == a.GameName && 
                                ca.TagLine == a.TagLine && 
                                ca.AccountName == a.AccountName)).ToList();
                        
                        foreach (var account in accountsToRemove)
                        {
                            _mainViewModel.Accounts.Remove(account);
                            LogInformation($"Removed extra account from MainViewModel: {account.GameName}#{account.TagLine}");
                        }
                        
                        // Do the same for LoginViewModel
                        foreach (var account in currentAccounts)
                        {
                            if (!_mainViewModel.LoginViewModel.LoginAccounts.Any(a => 
                                a.GameName == account.GameName && 
                                a.TagLine == account.TagLine && 
                                a.AccountName == account.AccountName))
                            {
                                _mainViewModel.LoginViewModel.LoginAccounts.Add(account);
                                LogInformation($"Added missing account to LoginViewModel: {account.GameName}#{account.TagLine}");
                            }
                        }
                        
                        var loginAccountsToRemove = _mainViewModel.LoginViewModel.LoginAccounts.Where(a => 
                            !currentAccounts.Any(ca => 
                                ca.GameName == a.GameName && 
                                ca.TagLine == a.TagLine && 
                                ca.AccountName == a.AccountName)).ToList();
                        
                        foreach (var account in loginAccountsToRemove)
                        {
                            _mainViewModel.LoginViewModel.LoginAccounts.Remove(account);
                            LogInformation($"Removed extra account from LoginViewModel: {account.GameName}#{account.TagLine}");
                        }
                        
                        // Update account count and timestamp
                        _mainViewModel.AccountCount = currentAccounts.Count;
                        _mainViewModel.LastUpdated = DateTime.Now;
                        
                        LogInformation($"Successfully synchronized {currentAccounts.Count} accounts between all tabs on UI thread");
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Error during UI synchronization");
                    }
                });
            }
            catch (Exception ex)
            {
                LogError(ex, "Error during forced synchronization");
            }
        }

        /// <summary>
        /// Debug method to check synchronization status
        /// </summary>
        public void DebugSynchronizationStatus()
        {
            try
            {
                LogInformation("=== DEBUG SYNCHRONIZATION STATUS ===");
                LogInformation($"ManageViewModel Accounts Count: {Accounts.Count}");
                
                if (_mainViewModel != null)
                {
                    LogInformation($"MainViewModel Accounts Count: {_mainViewModel.Accounts.Count}");
                    LogInformation($"LoginViewModel Accounts Count: {_mainViewModel.LoginViewModel.LoginAccounts.Count}");
                    
                    // Log account details for comparison
                    LogInformation("ManageViewModel Accounts:");
                    foreach (var account in Accounts)
                    {
                        LogInformation($"  - {account.GameName}#{account.TagLine} ({account.AccountName})");
                    }
                    
                    LogInformation("MainViewModel Accounts:");
                    foreach (var account in _mainViewModel.Accounts)
                    {
                        LogInformation($"  - {account.GameName}#{account.TagLine} ({account.AccountName})");
                    }
                    
                    LogInformation("LoginViewModel Accounts:");
                    foreach (var account in _mainViewModel.LoginViewModel.LoginAccounts)
                    {
                        LogInformation($"  - {account.GameName}#{account.TagLine} ({account.AccountName})");
                    }
                }
                else
                {
                    LogWarning("MainViewModel is null - cannot check synchronization");
                }
                
                LogInformation("=== END DEBUG SYNCHRONIZATION STATUS ===");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error during synchronization status debug");
            }
        }

        /// <summary>
        /// Test method to verify synchronization is working
        /// </summary>
        public void TestSynchronization()
        {
            try
            {
                LogInformation("=== TESTING SYNCHRONIZATION ===");
                
                if (_mainViewModel == null)
                {
                    LogWarning("MainViewModel is null - synchronization test failed");
                    return;
                }
                
                // Test adding a dummy account
                var testAccount = new Account
                {
                    GameName = "TEST",
                    TagLine = "1234",
                    AccountName = "testuser",
                    Region = "AP"
                };
                
                LogInformation("Adding test account to ManageViewModel...");
                Accounts.Add(testAccount);
                
                LogInformation("Checking if test account appears in other ViewModels...");
                var inMainViewModel = _mainViewModel.Accounts.Any(a => a.GameName == "TEST");
                var inLoginViewModel = _mainViewModel.LoginViewModel.LoginAccounts.Any(a => a.GameName == "TEST");
                
                LogInformation($"Test account in MainViewModel: {inMainViewModel}");
                LogInformation($"Test account in LoginViewModel: {inLoginViewModel}");
                
                // Remove test account
                Accounts.Remove(testAccount);
                
                LogInformation("=== SYNCHRONIZATION TEST COMPLETED ===");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error during synchronization test");
            }
        }

        /// <summary>
        /// Test method to verify account deletion is working
        /// </summary>
        public void TestAccountDeletion()
        {
            try
            {
                LogInformation("=== TESTING ACCOUNT DELETION ===");
                
                if (_mainViewModel == null)
                {
                    LogWarning("MainViewModel is null - deletion test failed");
                    return;
                }
                
                // Create a test account
                var testAccount = new Account
                {
                    GameName = "DELETETEST",
                    TagLine = "9999",
                    AccountName = "deletetestuser",
                    Region = "AP"
                };
                
                LogInformation("Adding test account for deletion test...");
                Accounts.Add(testAccount);
                
                // Simulate selection
                SelectedAccount = testAccount;
                
                LogInformation("Testing account deletion...");
                DeleteAccount();
                
                LogInformation("Checking if test account was removed from all ViewModels...");
                var inManageViewModel = Accounts.Any(a => a.GameName == "DELETETEST");
                var inMainViewModel = _mainViewModel.Accounts.Any(a => a.GameName == "DELETETEST");
                var inLoginViewModel = _mainViewModel.LoginViewModel.LoginAccounts.Any(a => a.GameName == "DELETETEST");
                
                LogInformation($"Test account in ManageViewModel: {inManageViewModel}");
                LogInformation($"Test account in MainViewModel: {inMainViewModel}");
                LogInformation($"Test account in LoginViewModel: {inLoginViewModel}");
                
                LogInformation("=== ACCOUNT DELETION TEST COMPLETED ===");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error during account deletion test");
            }
        }

        /// <summary>
        /// Force clear password field - useful for debugging password binding issues
        /// </summary>
        public void ForceClearPassword()
        {
            try
            {
                LogInformation("Force clearing password field...");
                
                // Force clear password
                Password = string.Empty;
                
                // Also clear other fields to ensure form is completely reset
                GameName = string.Empty;
                TagLine = string.Empty;
                Username = string.Empty;
                Region = "AP";
                AvatarPath = string.Empty;
                SelectedAccount = null;
                IsEditing = false;
                
                LogInformation("Password and form fields force cleared");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error force clearing password");
            }
        }

        #endregion
    } 
} 