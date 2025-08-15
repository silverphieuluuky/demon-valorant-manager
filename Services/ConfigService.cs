using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RiotAutoLogin.Services
{
    public class ConfigService
    {
        private static readonly string ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RiotClientAutoLogin");
        
        private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");
        
        private static readonly ILogger _logger = LoggingService.GetLogger<ConfigService>();
        
        public static AppConfig LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    // Create default config if file doesn't exist
                    var defaultConfig = CreateDefaultConfig();
                    SaveConfig(defaultConfig);
                    _logger.LogInformation("Created default config file");
                    return defaultConfig;
                }

                string json = File.ReadAllText(ConfigFilePath);
                var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                
                if (config == null)
                {
                    _logger.LogWarning("Config file is corrupted, creating new default config");
                    var defaultConfig = CreateDefaultConfig();
                    SaveConfig(defaultConfig);
                    return defaultConfig;
                }
                
                _logger.LogInformation("Config loaded successfully");
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading config, creating default config");
                var defaultConfig = CreateDefaultConfig();
                SaveConfig(defaultConfig);
                return defaultConfig;
            }
        }
        
        public static bool SaveConfig(AppConfig config)
        {
            try
            {
                if (config == null)
                    return false;

                // Ensure directory exists
                if (!Directory.Exists(ConfigDirectory))
                {
                    Directory.CreateDirectory(ConfigDirectory);
                    _logger.LogDebug("Created config directory: {ConfigDirectory}", ConfigDirectory);
                }

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
                
                _logger.LogInformation("Config saved successfully to: {ConfigFilePath}", ConfigFilePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving config");
                return false;
            }
        }
        
        private static AppConfig CreateDefaultConfig()
        {
            return new AppConfig
            {
                AutoStartEnabled = false,
                MinimizeToTrayEnabled = true,
                LastUpdated = DateTime.UtcNow,
                Version = "1.1.0"
            };
        }
        
        public static void UpdateSetting<T>(string propertyName, T value)
        {
            try
            {
                var config = LoadConfig();
                var property = typeof(AppConfig).GetProperty(propertyName);
                
                if (property != null && property.CanWrite)
                {
                    property.SetValue(config, value);
                    config.LastUpdated = DateTime.UtcNow;
                    SaveConfig(config);
                    _logger.LogInformation("Updated setting {PropertyName} to {Value}", propertyName, value);
                }
                else
                {
                    _logger.LogWarning("Property {PropertyName} not found or not writable", propertyName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting {PropertyName}", propertyName);
            }
        }
        
        // Test method to verify config service
        public static void TestConfigService()
        {
            try
            {
                _logger.LogInformation("Testing ConfigService...");
                
                // Test loading config
                var config = LoadConfig();
                _logger.LogInformation($"Config loaded - AutoStart: {config.AutoStartEnabled}, MinimizeToTray: {config.MinimizeToTrayEnabled}");
                
                // Test updating settings
                var newAutoStartValue = !config.AutoStartEnabled;
                UpdateSetting("AutoStartEnabled", newAutoStartValue);
                _logger.LogInformation($"Updated AutoStart to: {newAutoStartValue}");
                
                // Test loading again to verify
                var updatedConfig = LoadConfig();
                _logger.LogInformation($"Config reloaded - AutoStart: {updatedConfig.AutoStartEnabled}");
                
                // Revert the change
                UpdateSetting("AutoStartEnabled", !newAutoStartValue);
                _logger.LogInformation("Reverted AutoStart change");
                
                _logger.LogInformation("ConfigService test completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConfigService test failed");
            }
        }
    }
    
    public class AppConfig
    {
        public bool AutoStartEnabled { get; set; } = false;
        public bool MinimizeToTrayEnabled { get; set; } = true;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.1.0";
        
        // Add more settings as needed
        public bool EnableNotifications { get; set; } = true;
        public bool EnableSoundEffects { get; set; } = false;
        public string Theme { get; set; } = "Dark";
        public int RefreshInterval { get; set; } = 30; // minutes
    }
}
