using System;

namespace RiotAutoLogin.Interfaces
{
    public interface IConfigurationService
    {
        // Path management
        string GetConfigPath(string key);
        string GetAccountsFilePath();
        string GetApiKeyFilePath();
        string GetHenrikDevApiKeyFilePath();
        
        // Settings management
        T GetSetting<T>(string key, T defaultValue = default!);
        void SetSetting<T>(string key, T value);
        
        // App configuration
        AppConfig GetAppConfig();
        void SaveAppConfig(AppConfig config);
        
        // API key management
        string GetHenrikDevApiKey();
        bool SaveHenrikDevApiKey(string apiKey);
        bool HasHenrikDevApiKey();
    }
    
    public class AppConfig
    {
        public bool AutoStartEnabled { get; set; } = false;
        public bool MinimizeToTrayEnabled { get; set; } = true;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.1.0";
        public bool EnableNotifications { get; set; } = true;
        public bool EnableSoundEffects { get; set; } = false;
        public string Theme { get; set; } = "Dark";
        public int RefreshInterval { get; set; } = 30; // minutes
    }
}
