using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.Services;
using RiotAutoLogin.Interfaces;
using System.Windows;
using System;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using RiotAutoLogin.Constants;
using RiotAutoLogin.ViewModels;

namespace RiotAutoLogin.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private bool _autoStartEnabled = false;
        private bool _minimizeToTrayEnabled = false;
        private bool _isSaving = false;
        private string _henrikDevApiKey = string.Empty;
        private string _currentHenrikDevApiKey = string.Empty;

        private readonly IConfigurationService _configService;

        public SettingsViewModel(ILogger logger, IConfigurationService configService) : base(logger)
        {
            _configService = configService;
            LoadSettings();
            
            // Initialize commands
            SaveHenrikDevApiKeyCommand = new AsyncRelayCommand(SaveHenrikDevApiKey, () => !IsSaving);
            UpdateApiKeyCommand = new AsyncRelayCommand(UpdateApiKey, () => !IsSaving);
            ViewRankFetchLogCommand = new RelayCommand(ViewRankFetchLog);
            ClearRankFetchLogCommand = new RelayCommand(ClearRankFetchLog);
            ViewConfigFileCommand = new RelayCommand(ViewConfigFile);
        }

        public bool AutoStartEnabled
        {
            get => _autoStartEnabled;
            set 
            { 
                if (SetProperty(ref _autoStartEnabled, value))
                {
                    SaveGeneralSettings();
                }
            }
        }

        public bool MinimizeToTrayEnabled
        {
            get => _minimizeToTrayEnabled;
            set 
            { 
                if (SetProperty(ref _minimizeToTrayEnabled, value))
                {
                    SaveGeneralSettings();
                }
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public string HenrikDevApiKey
        {
            get => _henrikDevApiKey;
            set => SetProperty(ref _henrikDevApiKey, value);
        }

        public string CurrentHenrikDevApiKey
        {
            get => _currentHenrikDevApiKey;
            set => SetProperty(ref _currentHenrikDevApiKey, value);
        }

        public ICommand SaveHenrikDevApiKeyCommand { get; }
        public ICommand UpdateApiKeyCommand { get; }
        public ICommand ViewRankFetchLogCommand { get; }
        public ICommand ClearRankFetchLogCommand { get; }
        public ICommand ViewConfigFileCommand { get; }

        private void LoadSettings()
        {
            try
            {
                // Load current HenrikDev API key
                var henrikDevApiKey = ApiKeyManager.GetHenrikDevApiKey();
                
                // Display current key (masked for security)
                CurrentHenrikDevApiKey = !string.IsNullOrEmpty(henrikDevApiKey) ? MaskApiKey(henrikDevApiKey) : "Not configured";
                
                // Load general settings from config file
                var config = ConfigService.LoadConfig();
                AutoStartEnabled = config.AutoStartEnabled;
                MinimizeToTrayEnabled = config.MinimizeToTrayEnabled;
                
                LogInformation("Settings loaded successfully from config file");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error loading settings");
            }
        }
        
        private void SaveGeneralSettings()
        {
            try
            {
                // Save general settings to config file
                ConfigService.UpdateSetting("AutoStartEnabled", AutoStartEnabled);
                ConfigService.UpdateSetting("MinimizeToTrayEnabled", MinimizeToTrayEnabled);
                
                LogInformation($"General settings saved to config file - AutoStart: {AutoStartEnabled}, MinimizeToTray: {MinimizeToTrayEnabled}");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error saving general settings");
            }
        }

        private string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 8)
                return "Not configured";
            
            // Show first 6 and last 4 characters for better readability
            if (apiKey.Length <= 10)
                return apiKey.Substring(0, 4) + "..." + apiKey.Substring(apiKey.Length - 2);
            
            return apiKey.Substring(0, 6) + "..." + apiKey.Substring(apiKey.Length - 4);
        }



        private async Task SaveHenrikDevApiKey()
        {
            try
            {
                IsSaving = true;
                LogInformation("Saving HenrikDev API key...");

                if (string.IsNullOrWhiteSpace(HenrikDevApiKey))
                {
                    MessageBox.Show("Please enter a valid HenrikDev API key.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save the API key
                ApiKeyManager.SaveHenrikDevApiKey(HenrikDevApiKey);
                
                // Update display
                CurrentHenrikDevApiKey = MaskApiKey(HenrikDevApiKey);
                HenrikDevApiKey = string.Empty; // Clear input field
                
                LogInformation("HenrikDev API key saved successfully");
                MessageBox.Show("HenrikDev API key saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                await Task.Delay(1); // Make method properly async
            }
            catch (Exception ex)
            {
                LogError(ex, "Error saving HenrikDev API key");
                MessageBox.Show($"Error saving HenrikDev API key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        private async Task UpdateApiKey()
        {
            try
            {
                IsSaving = true;
                LogInformation("Refreshing API key display...");

                // Reload current HenrikDev API key
                var henrikDevApiKey = ApiKeyManager.GetHenrikDevApiKey();
                
                // Update display
                CurrentHenrikDevApiKey = !string.IsNullOrEmpty(henrikDevApiKey) ? MaskApiKey(henrikDevApiKey) : "Not configured";
                
                LogInformation("API key display refreshed successfully");
                MessageBox.Show("API key display refreshed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                await Task.Delay(1); // Make method properly async
            }
            catch (Exception ex)
            {
                LogError(ex, "Error refreshing API key display");
                MessageBox.Show($"Error refreshing API key display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void ViewRankFetchLog()
        {
            try
            {
                var logContent = RankFetchLogger.GetLogContent();
                var logFilePath = RankFetchLogger.GetLogFilePath();
                
                var message = $"Rank Fetch Log File: {logFilePath}\n\n" +
                             $"Log Content:\n{logContent}";
                
                // Show in a message box for now, could be enhanced with a proper dialog
                System.Windows.MessageBox.Show(
                    message,
                    "Rank Fetch Log",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                
                LogInformation("Rank fetch log viewed");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error viewing rank fetch log");
                System.Windows.MessageBox.Show(
                    $"Error viewing log: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void ClearRankFetchLog()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "Are you sure you want to clear the rank fetch log file?",
                    "Clear Log",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    RankFetchLogger.ClearLogFile();
                    LogInformation("Rank fetch log cleared");
                    System.Windows.MessageBox.Show(
                        "Rank fetch log has been cleared successfully.",
                        "Log Cleared",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error clearing rank fetch log");
                System.Windows.MessageBox.Show(
                    $"Error clearing log: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        
        private void ViewConfigFile()
        {
            try
            {
                var config = ConfigService.LoadConfig();
                var configFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RiotClientAutoLogin", "config.json");
                
                var message = $"Config File: {configFilePath}\n\n" +
                             $"Auto Start: {config.AutoStartEnabled}\n" +
                             $"Minimize to Tray: {config.MinimizeToTrayEnabled}\n" +
                             $"Enable Notifications: {config.EnableNotifications}\n" +
                             $"Enable Sound Effects: {config.EnableSoundEffects}\n" +
                             $"Theme: {config.Theme}\n" +
                             $"Refresh Interval: {config.RefreshInterval} minutes\n" +
                             $"Version: {config.Version}\n" +
                             $"Last Updated: {config.LastUpdated:yyyy-MM-dd HH:mm:ss}";
                
                System.Windows.MessageBox.Show(
                    message,
                    "Config File Info",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                
                LogInformation("Config file info viewed");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error viewing config file");
                System.Windows.MessageBox.Show(
                    $"Error viewing config: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
} 