using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace RiotAutoLogin.Services
{
    public static class ServerSettingsService
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RiotClientAutoLogin", "server_settings.json");

        private static readonly ILogger _logger = LoggingService.GetLogger("ServerSettingsService");

        public static string GetDefaultServer()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    // Return default server if settings file doesn't exist
                    return "AP";
                }

                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<ServerSettings>(json) ?? new ServerSettings();
                return settings.DefaultServer ?? "AP";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading server settings");
                return "AP"; // Return default server on error
            }
        }

        public static bool SaveDefaultServer(string defaultServer)
        {
            try
            {
                var settings = new ServerSettings
                {
                    DefaultServer = defaultServer,
                    LastUpdated = DateTime.UtcNow
                };

                string? directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);

                _logger.LogInformation($"Saved default server: {defaultServer}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving server settings");
                return false;
            }
        }

        public static ServerSettings GetServerSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return new ServerSettings
                    {
                        DefaultServer = "AP",
                        LastUpdated = DateTime.UtcNow
                    };
                }

                string json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<ServerSettings>(json) ?? new ServerSettings();
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading server settings");
                return new ServerSettings
                {
                    DefaultServer = "AP",
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        public static bool SaveServerSettings(ServerSettings settings)
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);

                _logger.LogInformation($"Saved server settings: {settings.DefaultServer}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving server settings");
                return false;
            }
        }
    }

    public class ServerSettings
    {
        public string DefaultServer { get; set; } = "AP";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> ServerInfo { get; set; } = new Dictionary<string, string>
        {
            { "AP", "Asia Pacific" },
            { "NA", "North America" },
            { "EU", "Europe" },
            { "KR", "Korea" },
            { "BR", "Brazil" },
            { "LATAM", "Latin America" }
        };
    }
}
