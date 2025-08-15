using RiotAutoLogin.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace RiotAutoLogin.Services
{
    public class OptimizedConfigurationService : IConfigurationService
    {
        private readonly ILogger _logger;
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly Dictionary<string, object> _cache = new();
        private readonly SemaphoreSlim _cacheLock = new(1, 1);
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);
        private readonly Dictionary<string, DateTime> _cacheTimestamps = new();

        public OptimizedConfigurationService(ILogger logger)
        {
            _logger = logger;
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RiotClientAutoLogin");
            _configFilePath = Path.Combine(_configDirectory, "config.json");
            
            // Ensure directory exists
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
                _logger.LogInformation("Created config directory: {ConfigDirectory}", _configDirectory);
            }
        }

        public string GetConfigPath(string key)
        {
            return key switch
            {
                "accounts" => GetAccountsFilePath(),
                "apikey" => GetApiKeyFilePath(),
                "henrikdev_apikey" => GetHenrikDevApiKeyFilePath(),
                "config" => _configFilePath,
                _ => Path.Combine(_configDirectory, $"{key}.json")
            };
        }

        public string GetAccountsFilePath()
        {
            return Path.Combine(_configDirectory, "accounts.json");
        }

        public string GetApiKeyFilePath()
        {
            return Path.Combine(_configDirectory, "apikey.txt");
        }

        public string GetHenrikDevApiKeyFilePath()
        {
            return Path.Combine(_configDirectory, "henrikdev_apikey.txt");
        }

        public T GetSetting<T>(string key, T defaultValue = default!)
        {
            try
            {
                _cacheLock.Wait();
                
                // Check cache first
                if (_cache.TryGetValue(key, out var cachedValue) && 
                    _cacheTimestamps.TryGetValue(key, out var timestamp) &&
                    DateTime.UtcNow - timestamp < _cacheExpiry)
                {
                    return (T)cachedValue;
                }

                // Load from file
                var config = GetAppConfig();
                var property = typeof(Interfaces.AppConfig).GetProperty(key);
                
                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(config);
                    if (value != null)
                    {
                        // Cache the value
                        _cache[key] = value;
                        _cacheTimestamps[key] = DateTime.UtcNow;
                        return (T)value;
                    }
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting setting {Key}", key);
                return defaultValue;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public void SetSetting<T>(string key, T value)
        {
            try
            {
                var config = GetAppConfig();
                var property = typeof(Interfaces.AppConfig).GetProperty(key);
                
                if (property != null && property.CanWrite)
                {
                    property.SetValue(config, value);
                    config.LastUpdated = DateTime.UtcNow;
                    SaveAppConfig(config);
                    
                    // Update cache
                    _cacheLock.Wait();
                    _cache[key] = value!;
                    _cacheTimestamps[key] = DateTime.UtcNow;
                    _cacheLock.Release();
                    
                    _logger.LogInformation("Updated setting {Key} to {Value}", key, value);
                }
                else
                {
                    _logger.LogWarning("Property {Key} not found or not writable", key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting {Key}", key);
            }
        }

        public Interfaces.AppConfig GetAppConfig()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    var defaultConfig = CreateDefaultConfig();
                    SaveAppConfig(defaultConfig);
                    _logger.LogInformation("Created default config file");
                    return defaultConfig;
                }

                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<Interfaces.AppConfig>(json) ?? new Interfaces.AppConfig();
                
                if (config == null)
                {
                    _logger.LogWarning("Config file is corrupted, creating new default config");
                    var defaultConfig = CreateDefaultConfig();
                    SaveAppConfig(defaultConfig);
                    return defaultConfig;
                }
                
                _logger.LogDebug("Config loaded successfully");
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading config, creating default config");
                var defaultConfig = CreateDefaultConfig();
                SaveAppConfig(defaultConfig);
                return defaultConfig;
            }
        }

        public void SaveAppConfig(Interfaces.AppConfig config)
        {
            try
            {
                if (config == null)
                    return;

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configFilePath, json);
                
                _logger.LogDebug("Config saved successfully to: {ConfigFilePath}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving config");
            }
        }

        public string GetHenrikDevApiKey()
        {
            try
            {
                var apiKeyPath = GetHenrikDevApiKeyFilePath();
                if (File.Exists(apiKeyPath))
                {
                    return File.ReadAllText(apiKeyPath).Trim();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading HenrikDev API key");
                return string.Empty;
            }
        }

        public bool SaveHenrikDevApiKey(string apiKey)
        {
            try
            {
                var apiKeyPath = GetHenrikDevApiKeyFilePath();
                var directory = Path.GetDirectoryName(apiKeyPath);
                
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(apiKeyPath, apiKey.Trim());
                _logger.LogInformation("HenrikDev API key saved successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving HenrikDev API key");
                return false;
            }
        }

        public bool HasHenrikDevApiKey()
        {
            return !string.IsNullOrEmpty(GetHenrikDevApiKey());
        }

        private Interfaces.AppConfig CreateDefaultConfig()
        {
            return new Interfaces.AppConfig
            {
                AutoStartEnabled = false,
                MinimizeToTrayEnabled = true,
                LastUpdated = DateTime.UtcNow,
                Version = "1.1.0",
                EnableNotifications = true,
                EnableSoundEffects = false,
                Theme = "Dark",
                RefreshInterval = 30
            };
        }

        public void ClearCache()
        {
            try
            {
                _cacheLock.Wait();
                _cache.Clear();
                _cacheTimestamps.Clear();
                _logger.LogDebug("Configuration cache cleared");
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    }
}
