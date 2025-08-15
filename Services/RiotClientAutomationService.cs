using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using RiotAutoLogin.Models;
using RiotAutoLogin.Services;
using RiotAutoLogin.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace RiotAutoLogin.Services
{
    using Application = FlaUI.Core.Application;
    public class RiotClientAutomationService : IRiotClientAutomationService
    {
        private readonly HttpClient _httpClient = new();
        private readonly ILogger _logger;

        public RiotClientAutomationService(ILogger logger)
        {
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> LoginAccountAsync(Account account)
        {
            try
            {
                _logger.LogInformation($"Starting login for account: {account.GameName}#{account.TagLine}");
                
                if (string.IsNullOrEmpty(account.AccountName) || string.IsNullOrEmpty(account.EncryptedPassword))
                {
                    _logger.LogWarning("Account missing required information");
                    return false;
                }

                // Decrypt password if needed
                string password = account.EncryptedPassword; // For now, assume it's already decrypted
                
                await LaunchAndLoginAsync(account.AccountName, password);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return false;
            }
        }

        public async Task LaunchAndLoginAsync(string username, string password)
        {
            _logger.LogInformation("Starting Riot Client process...");
            Process? riotClientProcess = null;

            Process[] processes = Process.GetProcessesByName("Riot Client");
            if (processes.Length > 0)
            {
                riotClientProcess = processes[0];
                _logger.LogInformation("Riot Client already running...");
            }
            else
            {
                _logger.LogInformation("Launching Riot Client...");
                try
                {
                    string riotClientPath = FindRiotClientPath();
                    if (string.IsNullOrEmpty(riotClientPath))
                    {
                        _logger.LogError("Riot Client not found in common installation locations");
                        System.Windows.MessageBox.Show(
                            "Riot Client not found in common installation locations:\n" +
                            "• C:\\Riot Games\\Riot Client\\RiotClientServices.exe\n" +
                            "• C:\\Program Files\\Riot Games\\Riot Client\\RiotClientServices.exe\n" +
                            "• C:\\Program Files (x86)\\Riot Games\\Riot Client\\RiotClientServices.exe\n\n" +
                            "Please make sure Riot Client is installed or manually start it before using auto-login.",
                            "Client Not Found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = riotClientPath,
                        Arguments = ""
                    };
                    riotClientProcess = Process.Start(startInfo);
                    _logger.LogInformation($"Riot Client launched from: {riotClientPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error launching Riot Client");
                    System.Windows.MessageBox.Show($"Error launching Riot Client: {ex.Message}", "Launch Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
            }

            Debug.WriteLine("Waiting for login form...");
            await Task.Delay(800); // Reduced from 1500ms to 800ms

            using (var automation = new UIA3Automation())
            {
                if (riotClientProcess != null && !riotClientProcess.HasExited)
                {
                    // Minimal wait for window interaction
                    await Task.Delay(200); // Reduced from 500ms to 200ms
                    await AutomateLoginAsync(riotClientProcess, automation, username, password);
                }
                else
                {
                    Debug.WriteLine("Riot Client process is not available or already exited.");
                    System.Windows.MessageBox.Show("Riot Client process is not available. Please try again.", 
                        "Login Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private static async Task AutomateLoginAsync(Process process, UIA3Automation automation, string username, string password)
        {
            var app = Application.Attach(process);
            var mainWindow = app.GetMainWindow(automation);
            if (mainWindow == null)
            {
                Debug.WriteLine("Could not find main window.");
                System.Windows.MessageBox.Show("Could not find Riot Client main window.", "Login Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Focus and bring Riot Client to front
            try
            {
                mainWindow.Focus();
                mainWindow.SetForeground();
                await Task.Delay(500); // Wait for window to be focused
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error focusing window: {ex.Message}");
            }

            var riotClientPane = mainWindow.FindFirstDescendant(
                cf => cf.ByName("Riot Client").And(cf.ByControlType(ControlType.Pane))
            );
            var parentElement = riotClientPane ?? mainWindow;

            // Find username field
            var usernameEdit = parentElement.FindFirstDescendant(
                cf => cf.ByAutomationId("username").And(cf.ByControlType(ControlType.Edit))
            );
            if (usernameEdit == null)
            {
                Debug.WriteLine("Username field not found.");
                System.Windows.MessageBox.Show("Could not find username field. Make sure Riot Client login screen is visible.", 
                    "Login Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Fill username with minimal timing
            try
            {
            usernameEdit.Focus();
            usernameEdit.Patterns.Value.Pattern.SetValue(string.Empty);
                usernameEdit.Patterns.Value.Pattern.SetValue(username); // No delay needed
                Debug.WriteLine("Username filled successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filling username: {ex.Message}");
                System.Windows.MessageBox.Show($"Error filling username: {ex.Message}", "Login Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Find password field
            var passwordEdit = parentElement.FindFirstDescendant(
                cf => cf.ByAutomationId("password").And(cf.ByControlType(ControlType.Edit))
            );
            if (passwordEdit == null)
            {
                Debug.WriteLine("Password field not found.");
                System.Windows.MessageBox.Show("Could not find password field. Make sure Riot Client login screen is visible.", 
                    "Login Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Fill password with minimal timing
            try
            {
            passwordEdit.Focus();
            passwordEdit.Patterns.Value.Pattern.SetValue(string.Empty);
                passwordEdit.Patterns.Value.Pattern.SetValue(password); // No delay needed
                Debug.WriteLine("Password filled successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filling password: {ex.Message}");
                System.Windows.MessageBox.Show($"Error filling password: {ex.Message}", "Login Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            // Submit login form with Enter key - much more reliable than button clicking
            try
            {
                Debug.WriteLine("Pressing Enter to submit login form...");
                
                // Small delay to ensure password field is fully filled
                await Task.Delay(100);
                
                // Make sure password field has focus and press Enter
                passwordEdit.Focus();
                await Task.Delay(50);
                
                // Send Enter key to submit the form
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                
                Debug.WriteLine("Enter key pressed successfully. Waiting for response...");
                await Task.Delay(500); // Wait for login response
                
                // Login attempt completed successfully
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error pressing Enter key: {ex.Message}");
                System.Windows.MessageBox.Show($"Error submitting login form: {ex.Message}", "Login Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public static async Task<string> VerifyAccountAsync(string gameName, string tagLine, string region)
        {
            if (string.IsNullOrEmpty(gameName) || string.IsNullOrEmpty(tagLine))
            {
                Debug.WriteLine("Invalid game name or tag line provided");
                return "Error: Invalid game name or tag line";
            }

            try
            {
                Debug.WriteLine($"Starting account verification for {gameName}#{tagLine}");
                
                var apiKey = await GetApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    Debug.WriteLine("No API key available");
                    return "Error: No API key available. Please set your Riot API key in Settings.";
                }

                Debug.WriteLine("API key found, proceeding with account verification");

                // Get account info by Riot ID
                var service = new RiotClientAutomationService(LoggingService.GetLogger<RiotClientAutomationService>());
                var puuid = await service.GetAccountByRiotIdAsync(gameName, tagLine, apiKey);
                if (puuid == null)
                {
                    Debug.WriteLine($"Account not found for {gameName}#{tagLine}");
                    return $"Error: Account '{gameName}#{tagLine}' not found. Please check spelling and ensure the account exists.";
                }

                Debug.WriteLine($"Account verified successfully for {gameName}#{tagLine}");
                return "Account verified successfully";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error verifying account for {gameName}#{tagLine}: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        private static async Task<string> GetApiKeyAsync()
        {
            return await Task.FromResult(RiotAutoLogin.Services.ApiKeyManager.GetApiKey());
        }

        private async Task<string?> GetAccountByRiotIdAsync(
            string gameName, string tagLine, string apiKey)
        {
            try
            {
                // Use the regional endpoint for account API (APAC region for account lookups)
                var url = $"https://asia.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}?api_key={apiKey}";
                
                Debug.WriteLine($"Making account API request: {url.Replace(apiKey, "***")}");
                var response = await _httpClient.GetStringAsync(url);
                
                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("puuid", out var puuidProp))
                {
                    string? puuid = puuidProp.GetString();
                    Debug.WriteLine($"Successfully got PUUID for {gameName}#{tagLine}");
                    return puuid ?? string.Empty;
                }
                
                Debug.WriteLine($"No PUUID found in response for {gameName}#{tagLine}");
                return null;
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"HTTP error getting account by Riot ID ({gameName}#{tagLine}): {httpEx.Message}");
                if (httpEx.Message.Contains("404"))
                {
                    Debug.WriteLine($"Account {gameName}#{tagLine} not found (404). Please check the spelling and region.");
                }
                else if (httpEx.Message.Contains("403"))
                {
                    Debug.WriteLine($"API key invalid or expired (403). Please check your API key.");
                }
                else if (httpEx.Message.Contains("429"))
                {
                    Debug.WriteLine($"Rate limit exceeded (429). Please wait before making more requests.");
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting account by Riot ID: {ex.Message}");
                return null;
            }
        }





        public static bool LaunchRiotClient(string username, string password, string region, bool rememberMe = true)
        {
            try
            {
                // Create temporary instance for static method
                var service = new RiotClientAutomationService(LoggingService.GetLogger<RiotClientAutomationService>());
                var riotClientPath = service.FindRiotClientPath();
                if (string.IsNullOrEmpty(riotClientPath))
                {
                    Debug.WriteLine("Riot Client not found");
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = riotClientPath,
                    Arguments = service.BuildLaunchArguments(username, password, region, rememberMe),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                Debug.WriteLine($"Riot Client launched with PID: {process?.Id}");
                return process != null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error launching Riot Client: {ex.Message}");
                return false;
            }
        }

        private string FindRiotClientPath()
        {
            // Common installation paths for Riot Client
            string[] possiblePaths = {
                @"C:\Riot Games\Riot Client\RiotClientServices.exe",
                @"C:\Program Files\Riot Games\Riot Client\RiotClientServices.exe", 
                @"C:\Program Files (x86)\Riot Games\Riot Client\RiotClientServices.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    @"Riot Games\Riot Client\RiotClientServices.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    @"Riot Games\Riot Client\RiotClientServices.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), 
                    @"Riot Games\Riot Client\RiotClientServices.exe")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    Debug.WriteLine($"Found Riot Client at: {path}");
                    return path;
                }
            }

            Debug.WriteLine("Riot Client not found in any common installation location.");
            return string.Empty;
        }

        private string BuildLaunchArguments(string username, string password, string region, bool rememberMe)
        {
            var args = new StringBuilder();
            args.Append("");
            args.Append($" --launch-patchline=live");
            
            if (!string.IsNullOrEmpty(username))
                args.Append($" --username=\"{username}\"");
            
            if (!string.IsNullOrEmpty(password))
                args.Append($" --password=\"{password}\"");
            
            if (!string.IsNullOrEmpty(region))
                args.Append($" --region=\"{region}\"");
            
            if (rememberMe)
                args.Append(" --remember-me");

            return args.ToString();
        }

        public async Task<bool> IsRiotClientRunningAsync()
        {
            try
            {
                await Task.CompletedTask; // Add await to satisfy async requirement
                var processes = Process.GetProcessesByName("RiotClientServices");
                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if Riot Client is running");
                return false;
            }
        }

        public async Task<bool> KillRiotClientAsync()
        {
            try
            {
                await Task.CompletedTask; // Add await to satisfy async requirement
                var processes = Process.GetProcessesByName("RiotClientServices");

                foreach (var process in processes)
                {
                    try
                    {
                        process.CloseMainWindow();
                        if (!process.WaitForExit(5000))
                            process.Kill();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error closing process {process.ProcessName}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                _logger.LogInformation("Riot Client processes terminated");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing Riot Client");
                return false;
            }
        }

        public static bool CloseRiotClient()
        {
            try
            {
                var processes = Process.GetProcessesByName("RiotClientServices");

                foreach (var process in processes)
                {
                    try
                    {
                        process.CloseMainWindow();
                        if (!process.WaitForExit(5000))
                            process.Kill();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error closing process {process.ProcessName}: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing Riot Client: {ex.Message}");
                return false;
            }
        }

        public static void ShowRiotClient()
        {
            try
            {
                var process = Process.GetProcessesByName("RiotClientUx")
                    .FirstOrDefault();

                if (process != null)
                {
                    var hWnd = process.MainWindowHandle;
                    if (hWnd != IntPtr.Zero)
                    {
                        ShowWindow(hWnd, 9); // SW_RESTORE
                        SetForegroundWindow(hWnd);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing Riot Client: {ex.Message}");
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
