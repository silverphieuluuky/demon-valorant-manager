using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.Models;
using RiotAutoLogin.Interfaces;

namespace RiotAutoLogin.Services
{
    public class AsyncAccountService : IAccountService
    {
        private readonly ILogger<AsyncAccountService> _logger;
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RiotClientAutoLogin", "accounts.json");

        public AsyncAccountService(ILogger<AsyncAccountService> logger)
        {
            _logger = logger;
        }

        public List<Account> LoadAccounts()
        {
            // Fallback to sync method for backward compatibility
            return LoadAccounts(ConfigFilePath);
        }

        public List<Account> LoadAccounts(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                    return new List<Account>();

                string json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<List<Account>>(json) ?? new List<Account>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading accounts synchronously");
                return new List<Account>();
            }
        }

        public async Task<List<Account>> LoadAccountsAsync()
        {
            return await LoadAccountsAsync(ConfigFilePath);
        }

        public async Task<List<Account>> LoadAccountsAsync(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                    return new List<Account>();

                using var stream = File.OpenRead(configPath);
                var accounts = await JsonSerializer.DeserializeAsync<List<Account>>(stream);
                var result = accounts ?? new List<Account>();
                _logger.LogInformation("Loaded {Count} accounts asynchronously", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading accounts asynchronously");
                return new List<Account>();
            }
        }

        public bool SaveAccounts(List<Account> accounts)
        {
            return SaveAccounts(accounts, ConfigFilePath);
        }

        public bool SaveAccounts(List<Account> accounts, string configPath)
        {
            try
            {
                if (accounts == null)
                {
                    _logger.LogWarning("Accounts list is null");
                    return false;
                }

                string? directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                
                _logger.LogInformation("Saved {Count} accounts synchronously", accounts.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving accounts synchronously");
                return false;
            }
        }

        public async Task<bool> SaveAccountsAsync(List<Account> accounts)
        {
            return await SaveAccountsAsync(accounts, ConfigFilePath);
        }

        public async Task<bool> SaveAccountsAsync(List<Account> accounts, string configPath)
        {
            try
            {
                if (accounts == null)
                {
                    _logger.LogWarning("Accounts list is null");
                    return false;
                }

                string? directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }

                using var stream = File.Create(configPath);
                await JsonSerializer.SerializeAsync(stream, accounts, new JsonSerializerOptions { WriteIndented = true });
                
                _logger.LogInformation("Saved {Count} accounts asynchronously", accounts.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving accounts asynchronously");
                return false;
            }
        }

        public async Task UpdateAllAccountsAsync(List<Account> accounts)
        {
            _logger.LogInformation("Updating ranks for {Count} accounts asynchronously", accounts.Count);
            
            var updateTasks = accounts.Select(async account =>
            {
                try
                {
                    await UpdateAccountRanksAsync(account);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating rank for account {GameName}", account.GameName);
                    return false;
                }
            });
            
            var results = await Task.WhenAll(updateTasks);
            
            int successCount = results.Count(r => r);
            int failCount = results.Count(r => !r);
            
            _logger.LogInformation("Rank update summary: {Success} succeeded, {Failed} failed", successCount, failCount);
            
            // Save accounts after updating ranks
            await SaveAccountsAsync(accounts);
        }

        public async Task UpdateAccountRanksAsync(Account account)
        {
            // Store old rank data in case of failure
            var oldCurrentRank = account.CurrentRank;
            var oldPeakRank = account.PeakRank;
            var oldRankRating = account.RankRating;
            
            try
            {
                // Check if we need to fetch rank
                if (ShouldSkipRankFetch(account))
                {
                    _logger.LogDebug("Skipping rank fetch for {GameName} - rank was updated recently", account.GameName);
                    return;
                }
                
                // Clear old rank data first
                account.CurrentRank = string.Empty;
                account.PeakRank = string.Empty;
                account.IsRankLoading = false;
                account.IsRankFailed = false;
                account.LastError = string.Empty;
                
                var henrikDevService = new HenrikDevService();
                var apiKey = ApiKeyManager.GetHenrikDevApiKey();
                
                if (string.IsNullOrEmpty(apiKey))
                {
                    account.CurrentRank = "Unranked";
                    account.IsRankFailed = true;
                    account.LastError = "HenrikDev API key not configured";
                    _logger.LogWarning("No HenrikDev API key for {GameName}", account.GameName);
                    return;
                }
                
                // Use account region or default server if not set
                var region = !string.IsNullOrEmpty(account.Region) ? account.Region : ServerSettingsService.GetDefaultServer();
                
                var profile = await henrikDevService.GetPlayerProfileAsync(
                    account.GameName, 
                    account.TagLine, 
                    region, 
                    apiKey,
                    maxRetries: 3
                );
                
                if (profile != null && !string.IsNullOrEmpty(profile.CurrentRank) && profile.CurrentRank != "Unknown")
                {
                    account.CurrentRank = profile.CurrentRank;
                    account.PeakRank = profile.PeakRank;
                    account.RankRating = profile.RankRating;
                    account.LastRankUpdate = DateTime.UtcNow;
                    account.LastError = string.Empty;
                    
                    // Check if rank is valid
                    var rank = profile.CurrentRank.Trim().ToLower();
                    if (rank != "unrated" && rank != "unranked" && rank != "unknown")
                    {
                        account.IsRankLoaded = true;
                        account.IsRankFailed = false;
                        _logger.LogInformation("Updated rank for {GameName}: {Rank} (MMR: {Rating}) using server: {Region}", 
                            account.GameName, profile.CurrentRank, profile.RankRating, region);
                        
                        // Save the account immediately after successful rank update
                        await ForceSaveRankDataAsync(account);
                    }
                    else
                    {
                        account.IsRankLoaded = false;
                        account.IsRankFailed = false;
                        account.LastError = string.Empty;
                        _logger.LogInformation("Account {GameName} has no actual rank: {Rank} using server: {Region}", 
                            account.GameName, profile.CurrentRank, region);
                        
                        await ForceSaveRankDataAsync(account);
                    }
                }
                else
                {
                    account.CurrentRank = "Unranked";
                    account.PeakRank = string.Empty;
                    account.IsRankLoaded = false;
                    account.IsRankFailed = true;
                    account.LastError = profile == null ? "No response from API" : "No rank data available";
                    _logger.LogWarning("No rank data for {GameName}, set to Unranked using server: {Region}", 
                        account.GameName, region);
                    
                    await ForceSaveRankDataAsync(account);
                }
            }
            catch (Exception ex)
            {
                // Restore old rank data
                RestoreOldRankData(account, oldCurrentRank, oldPeakRank, oldRankRating);
                account.IsRankFailed = true;
                account.LastError = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error updating rank for {GameName}, keeping old rank: {OldRank}", 
                    account.GameName, oldCurrentRank);
                
                await ForceSaveRankDataAsync(account);
            }
        }

        public bool ForceSaveRankData(Account account)
        {
            // Fallback to sync method
            try
            {
                var allAccounts = LoadAccounts();
                var existingAccount = allAccounts.FirstOrDefault(a => 
                    string.Equals(a.GameName?.Trim(), account.GameName?.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(a.TagLine?.Trim(), account.TagLine?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (existingAccount != null)
                {
                    UpdateAccountRankData(existingAccount, account);
                }
                else
                {
                    allAccounts.Add(account);
                }
                
                return SaveAccounts(allAccounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force saving rank data synchronously");
                return false;
            }
        }

        public async Task<bool> ForceSaveRankDataAsync(Account account)
        {
            try
            {
                var allAccounts = await LoadAccountsAsync();
                var existingAccount = allAccounts.FirstOrDefault(a => 
                    string.Equals(a.GameName?.Trim(), account.GameName?.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(a.TagLine?.Trim(), account.TagLine?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (existingAccount != null)
                {
                    UpdateAccountRankData(existingAccount, account);
                }
                else
                {
                    allAccounts.Add(account);
                }
                
                return await SaveAccountsAsync(allAccounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force saving rank data asynchronously");
                return false;
            }
        }

        private static bool ShouldSkipRankFetch(Account account)
        {
            // If rank was fetched in the last 6 hours, skip
            if (account.LastRankUpdate != DateTime.MinValue)
            {
                var timeSinceLastUpdate = DateTime.UtcNow - account.LastRankUpdate;
                if (timeSinceLastUpdate.TotalHours < 6)
                {
                    return true;
                }
            }

            // If rank is valid and not too old, skip
            if (!string.IsNullOrEmpty(account.CurrentRank) && 
                account.CurrentRank != "Unranked" && 
                account.CurrentRank != "Unknown" &&
                account.CurrentRank != "Failed to load" &&
                account.LastRankUpdate != DateTime.MinValue)
            {
                var timeSinceLastUpdate = DateTime.UtcNow - account.LastRankUpdate;
                if (timeSinceLastUpdate.TotalHours < 24)
                {
                    return true;
                }
            }
            
            // NEVER skip if rank is "Failed to load"
            if (account.CurrentRank == "Failed to load")
            {
                return false;
            }

            return false;
        }

        private static void RestoreOldRankData(Account account, string oldCurrentRank, string oldPeakRank, int oldRankRating)
        {
            if (!string.IsNullOrEmpty(oldCurrentRank) && 
                oldCurrentRank != "Unranked" && 
                oldCurrentRank != "Unknown" &&
                oldCurrentRank != "Failed to load")
            {
                account.CurrentRank = oldCurrentRank;
                account.PeakRank = oldPeakRank;
                account.RankRating = oldRankRating;
                account.IsRankLoaded = true;
                account.IsRankFailed = false;
            }
            else
            {
                account.CurrentRank = "Failed to load";
                account.PeakRank = string.Empty;
                account.IsRankLoaded = false;
                account.IsRankFailed = true;
            }
        }

        private static void UpdateAccountRankData(Account existingAccount, Account newData)
        {
            existingAccount.CurrentRank = newData.CurrentRank;
            existingAccount.PeakRank = newData.PeakRank;
            existingAccount.RankRating = newData.RankRating;
            existingAccount.LastRankUpdate = newData.LastRankUpdate;
            existingAccount.IsRankLoaded = newData.IsRankLoaded;
            existingAccount.IsRankFailed = newData.IsRankFailed;
            existingAccount.LastError = newData.LastError;
        }
    }
}
