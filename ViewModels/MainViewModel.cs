using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.Models;
using RiotAutoLogin.Services;
using RiotAutoLogin.Interfaces;
using RiotAutoLogin.Constants;
using RiotAutoLogin.ViewModels;

namespace RiotAutoLogin.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private string _status = "Ready";
        private int _accountCount = 0;
        private DateTime _lastUpdated = DateTime.Now;
        private bool _isLoading = false;

        public ObservableCollection<Account> Accounts { get; } = new ObservableCollection<Account>();
        public LoginViewModel LoginViewModel { get; }
        public ManageViewModel ManageViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        public MainViewModel(ILogger logger) : base(logger)
        {
            LoginViewModel = new LoginViewModel(logger);
            ManageViewModel = new ManageViewModel(logger, App.GetService<IAccountService>());
            SettingsViewModel = new SettingsViewModel(logger, App.GetService<IConfigurationService>());

            // CRITICAL: Set MainViewModel reference in ManageViewModel for synchronization
            ManageViewModel.SetMainViewModel(this);

            // Initialize commands
            LoadAccountsCommand = new RelayCommand(async () => await LoadAccountsAsync());
            SaveAccountsCommand = new RelayCommand(async () => await SaveAccountsAsync());
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public int AccountCount
        {
            get => _accountCount;
            set => SetProperty(ref _accountCount, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoadAccountsCommand { get; }
        public ICommand SaveAccountsCommand { get; }

        // Properties for UIEventHandlers
        public Account? CurrentAccount { get; set; }
        public bool IsDeveloperMode { get; set; } = false;

        private async Task LoadAccountsAsync()
        {
            try
            {
                IsLoading = true;
                Status = "Loading accounts...";
                
                // Load accounts from AccountService asynchronously
                var accountService = App.GetService<IAccountService>();
                var loadedAccounts = await accountService.LoadAccountsAsync();
                
                LogInformation($"Loaded {loadedAccounts.Count} accounts from service, synchronizing ViewModels...");
                
                // Clear and reload collections
                Accounts.Clear();
                LoginViewModel.LoginAccounts.Clear();
                ManageViewModel.Accounts.Clear();
                
                foreach (var account in loadedAccounts)
                {
                    Accounts.Add(account);
                    LoginViewModel.LoginAccounts.Add(account);
                    ManageViewModel.Accounts.Add(account);
                }
                
                AccountCount = Accounts.Count;
                LastUpdated = DateTime.Now;
                Status = $"Loaded {AccountCount} accounts";
                
                LogInformation($"Successfully synchronized {AccountCount} accounts across all ViewModels");
                
                // Force synchronization to ensure all tabs are in sync
                ManageViewModel.ForceSynchronizeAccounts();
            }
            catch (Exception ex)
            {
                LogError(ex, "Error loading accounts");
                Status = "Error Loading";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveAccountsAsync()
        {
            try
            {
                IsLoading = true;
                Status = "Saving accounts...";
                
                // Save accounts asynchronously
                var accountService = App.GetService<IAccountService>();
                var success = await accountService.SaveAccountsAsync(Accounts.ToList());
                
                if (success)
                {
                    LastUpdated = DateTime.Now;
                    Status = $"Saved {AccountCount} accounts";
                    LogInformation($"Saved {AccountCount} accounts successfully asynchronously");
                }
                else
                {
                    Status = "Error saving accounts";
                    LogError(new Exception("Save operation returned false"), "Error saving accounts");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error saving accounts");
                Status = "Error saving accounts";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Methods for UIEventHandlers
        public void UpdateContentBasedOnTab()
        {
            // Implementation for tab content update
        }

        public void ToggleDeveloperMode()
        {
            IsDeveloperMode = !IsDeveloperMode;
        }

        public void StartLoginProcess()
        {
            // Implementation for login process
        }

        public void ShowHelp()
        {
            // Implementation for help display
        }

        public void SelectAccount(Account account)
        {
            CurrentAccount = account;
        }

        public void RefreshAllRanks()
        {
            // Implementation for refreshing all ranks
        }
    }
} 