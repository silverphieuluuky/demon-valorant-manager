using System;
using System.Diagnostics;
using System.IO;

namespace RiotAutoLogin.Services
{
    public static class ApiKeyManager
    {
        private static readonly string ApiKeyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RiotClientAutoLogin", "apikey.txt");
            
        private static readonly string HenrikDevApiKeyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RiotClientAutoLogin", "henrikdev_apikey.txt");

        public static string GetApiKey()
        {
            try
            {
                if (File.Exists(ApiKeyFilePath))
                {
                    return File.ReadAllText(ApiKeyFilePath).Trim();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading API key: {ex.Message}");
                return string.Empty;
            }
        }

        public static bool SaveApiKey(string apiKey)
        {
            try
            {
                string? directory = Path.GetDirectoryName(ApiKeyFilePath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(ApiKeyFilePath, apiKey.Trim());
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving API key: {ex.Message}");
                return false;
            }
        }
        
        public static string GetHenrikDevApiKey()
        {
            try
            {
                if (File.Exists(HenrikDevApiKeyFilePath))
                {
                    return File.ReadAllText(HenrikDevApiKeyFilePath).Trim();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading HenrikDev API key: {ex.Message}");
                return string.Empty;
            }
        }

        public static bool SaveHenrikDevApiKey(string apiKey)
        {
            try
            {
                string? directory = Path.GetDirectoryName(HenrikDevApiKeyFilePath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(HenrikDevApiKeyFilePath, apiKey.Trim());
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving HenrikDev API key: {ex.Message}");
                return false;
            }
        }
        
        public static bool HasHenrikDevApiKey()
        {
            return !string.IsNullOrEmpty(GetHenrikDevApiKey());
        }
    }
}
