using RiotAutoLogin.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace RiotAutoLogin.Services
{
    public static class AccountService
    {
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RiotClientAutoLogin", "accounts.json");

        public static List<Account> LoadAccounts()
        {
            return LoadAccounts(ConfigFilePath);
        }

        public static List<Account> LoadAccounts(string configPath)
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
                Debug.WriteLine($"Error loading accounts: {ex.Message}");
                return new List<Account>();
            }
        }

        public static bool SaveAccounts(List<Account> accounts)
        {
            return SaveAccounts(accounts, ConfigFilePath);
        }

        public static bool SaveAccounts(List<Account> accounts, string configPath)
        {
            try
            {
                Debug.WriteLine($"🔍 SaveAccounts: Starting to save {accounts?.Count ?? 0} accounts to {configPath}");
                
                if (accounts == null)
                {
                    Debug.WriteLine($"❌ SaveAccounts: Accounts list is null");
                    return false;
                }

                string? directory = Path.GetDirectoryName(configPath);
                Debug.WriteLine($"🔍 SaveAccounts: Directory path: {directory}");
                
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                    Debug.WriteLine($"🔍 SaveAccounts: Created directory: {directory}");
                }

                string json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true }) ?? "[]";
                Debug.WriteLine($"🔍 SaveAccounts: Serialized JSON length: {json.Length} characters");
                
                // Log first few characters of JSON for debugging
                if (json.Length > 100)
                {
                    Debug.WriteLine($"🔍 SaveAccounts: JSON preview: {json.Substring(0, 100)}...");
                }
                else
                {
                    Debug.WriteLine($"🔍 SaveAccounts: Full JSON: {json}");
                }
                
                File.WriteAllText(configPath, json);
                Debug.WriteLine($"💾 SaveAccounts: Successfully wrote {json.Length} characters to {configPath}");
                
                // Verify file was written
                if (File.Exists(configPath))
                {
                    var fileInfo = new FileInfo(configPath);
                    Debug.WriteLine($"🔍 SaveAccounts: File exists, size: {fileInfo.Length} bytes, last modified: {fileInfo.LastWriteTime}");
                }
                else
                {
                    Debug.WriteLine($"❌ SaveAccounts: File was not created at {configPath}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ SaveAccounts: Error saving accounts: {ex.Message}");
                Debug.WriteLine($"❌ SaveAccounts: Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        // UpdateAccountStatusAsync method removed - user doesn't want verification functionality

        public static async Task UpdateAllAccountsAsync(List<Account> accounts)
        {
            Debug.WriteLine($"🔄 Updating ranks for {accounts.Count} accounts...");
            
            var updateTasks = accounts.Select(async account =>
            {
                try
                {
                    Debug.WriteLine($"📈 Updating ranks for account {account.GameName}#{account.TagLine}...");
                    await UpdateAccountRanksAsync(account);
                    Debug.WriteLine($"✅ Successfully updated ranks for account {account.GameName}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Exception updating ranks for account {account.GameName}: {ex.Message}");
                    return false;
                }
            });
            
            var results = await Task.WhenAll(updateTasks);
            
            int successCount = results.Count(r => r);
            int failCount = results.Count(r => !r);
            
            Debug.WriteLine($"📊 Rank update summary: {successCount} succeeded, {failCount} failed out of {accounts.Count} accounts");
            
            // Save accounts after updating ranks to persist the changes
            try
            {
                SaveAccounts(accounts);
                Debug.WriteLine($"💾 Saved {accounts.Count} accounts with updated ranks");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error saving accounts after rank update: {ex.Message}");
            }
        }

        // UpdateAccountWithVerifyInfo method removed - user doesn't want verification functionality

        public static async Task UpdateAccountRanksAsync(Account account)
        {
            // Store old rank data in case of failure
            var oldCurrentRank = account.CurrentRank;
            var oldPeakRank = account.PeakRank;
            var oldRankRating = account.RankRating;
            
            try
            {
                // Check if we need to fetch rank (only if not fetched recently or if forced)
                if (ShouldSkipRankFetch(account))
                {
                    Debug.WriteLine($"⏭️ Skipping rank fetch for {account.GameName} - rank was updated recently");
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
                    Debug.WriteLine($"⚠️ No HenrikDev API key for {account.GameName}");
                    RankFetchLogger.LogRankFetchError(account, "HenrikDev API key not configured", "N/A");
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
                
                // Chỉ gán đúng rank trả về từ API
                if (profile != null && !string.IsNullOrEmpty(profile.CurrentRank) && profile.CurrentRank != "Unknown")
                {
                    account.CurrentRank = profile.CurrentRank;
                    account.PeakRank = profile.PeakRank; // Sử dụng peak rank từ API
                    account.RankRating = profile.RankRating;
                    account.LastRankUpdate = DateTime.UtcNow;
                    account.LastError = string.Empty;
                    
                    // Check if rank is valid (not unrated/unranked/unknown)
                    var rank = profile.CurrentRank.Trim().ToLower();
                    if (rank != "unrated" && rank != "unranked" && rank != "unknown")
                    {
                        account.IsRankLoaded = true;
                        account.IsRankFailed = false;
                        Debug.WriteLine($"✅ Updated rank for {account.GameName}: {profile.CurrentRank} (MMR: {profile.RankRating}) using server: {region}");
                        RankFetchLogger.LogRankFetchSuccess(account, profile.CurrentRank, profile.RankRating, region);
                        
                        // Save the account immediately after successful rank update
                        ForceSaveRankData(account);
                    }
                    else
                    {
                        // Valid response but no actual rank
                        account.IsRankLoaded = false;
                        account.IsRankFailed = false;
                        account.LastError = string.Empty;
                        Debug.WriteLine($"⚠️ Account {account.GameName} has no actual rank: {profile.CurrentRank} using server: {region}");
                        RankFetchLogger.LogRankFetchNoData(account, region, $"Account has rank: {profile.CurrentRank} but it's not a valid competitive rank");
                        
                        // Save the account with "no actual rank" status
                        ForceSaveRankData(account);
                    }
                }
                else
                {
                    // Nếu không có rank, gán đúng 'Unranked'
                    account.CurrentRank = "Unranked";
                    account.PeakRank = string.Empty;
                    account.IsRankLoaded = false;
                    account.IsRankFailed = true;
                    account.LastError = profile == null ? "No response from API" : "No rank data available";
                    Debug.WriteLine($"⚠️ No rank data for {account.GameName}, set to Unranked using server: {region}");
                    RankFetchLogger.LogRankFetchNoData(account, region, profile == null ? "No response from API" : "No rank data in response");
                    
                    // Save the account with "Unranked" status
                    ForceSaveRankData(account);
                }
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // Rate limit exceeded - restore old rank data
                RestoreOldRankData(account, oldCurrentRank, oldPeakRank, oldRankRating);
                account.IsRankFailed = true;
                account.LastError = "Rate limit exceeded - try again later";
                Debug.WriteLine($"⚠️ Rate limit exceeded for {account.GameName}, keeping old rank: {oldCurrentRank}");
                RankFetchLogger.LogRankFetchError(account, "Rate limit exceeded - try again later", account.Region, ex);
                
                // Save the restored rank data immediately
                ForceSaveRankData(account);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Invalid API key - restore old rank data
                RestoreOldRankData(account, oldCurrentRank, oldPeakRank, oldRankRating);
                account.IsRankFailed = true;
                account.LastError = "Invalid HenrikDev API key";
                Debug.WriteLine($"⚠️ Invalid HenrikDev API key for {account.GameName}, keeping old rank: {oldCurrentRank}");
                RankFetchLogger.LogRankFetchError(account, "Invalid HenrikDev API key", account.Region, ex);
                
                // Save the restored rank data immediately
                ForceSaveRankData(account);
            }
            catch (Exception ex)
            {
                // General error - restore old rank data
                RestoreOldRankData(account, oldCurrentRank, oldPeakRank, oldRankRating);
                account.IsRankFailed = true;
                account.LastError = $"Error: {ex.Message}";
                Debug.WriteLine($"❌ Error updating rank for {account.GameName}: {ex.Message}, keeping old rank: {oldCurrentRank}");
                RankFetchLogger.LogRankFetchError(account, ex.Message, account.Region, ex);
                
                // Save the restored rank data immediately
                ForceSaveRankData(account);
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
            
            // NEVER skip if rank is "Failed to load" - always try to fetch again
            if (account.CurrentRank == "Failed to load")
            {
                Debug.WriteLine($"🔄 Will fetch rank for {account.GameName} because current rank is 'Failed to load'");
                return false;
            }

            return false;
        }

        private static void RestoreOldRankData(Account account, string oldCurrentRank, string oldPeakRank, int oldRankRating)
        {
            // Restore old rank data if it exists and is valid
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
                Debug.WriteLine($"🔄 Restored old rank data for {account.GameName}: {oldCurrentRank}");
            }
            else
            {
                // If no valid old rank, set to "Failed to load"
                account.CurrentRank = "Failed to load";
                account.PeakRank = string.Empty;
                account.IsRankLoaded = false;
                account.IsRankFailed = true;
                Debug.WriteLine($"⚠️ No valid old rank for {account.GameName}, set to 'Failed to load'");
            }
        }
        
        private static void SaveRestoredRankData(Account account)
        {
            try
            {
                Debug.WriteLine($"🔍 SaveRestoredRankData: Starting to save restored rank data for {account.GameName}");
                Debug.WriteLine($"🔍 SaveRestoredRankData: CurrentRank = '{account.CurrentRank}', PeakRank = '{account.PeakRank}', RankRating = {account.RankRating}");
                
                // Load all accounts, update the specific account, and save
                var allAccounts = LoadAccounts();
                Debug.WriteLine($"🔍 SaveRestoredRankData: Loaded {allAccounts.Count} accounts from file");
                
                var existingAccount = allAccounts.FirstOrDefault(a => 
                    string.Equals(a.GameName?.Trim(), account.GameName?.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(a.TagLine?.Trim(), account.TagLine?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                // Log all accounts for debugging
                Debug.WriteLine($"🔍 SaveRestoredRankData: Searching for account '{account.GameName}#{account.TagLine}'");
                foreach (var acc in allAccounts)
                {
                    Debug.WriteLine($"🔍 SaveRestoredRankData: Available account: '{acc.GameName}#{acc.TagLine}' (GameName: '{acc.GameName}', TagLine: '{acc.TagLine}')");
                }
                
                if (existingAccount != null)
                {
                    Debug.WriteLine($"🔍 SaveRestoredRankData: Found existing account {existingAccount.GameName}, updating rank data");
                    
                    // Update the account with restored rank data
                    existingAccount.CurrentRank = account.CurrentRank;
                    existingAccount.PeakRank = account.PeakRank;
                    existingAccount.RankRating = account.RankRating;
                    existingAccount.LastRankUpdate = account.LastRankUpdate;
                    existingAccount.IsRankLoaded = account.IsRankLoaded;
                    existingAccount.IsRankFailed = account.IsRankFailed;
                    existingAccount.LastError = account.LastError;
                    
                    Debug.WriteLine($"🔍 SaveRestoredRankData: Updated existing account - CurrentRank = '{existingAccount.CurrentRank}', PeakRank = '{existingAccount.PeakRank}'");
                    
                    // Save all accounts
                    var saveResult = SaveAccounts(allAccounts);
                    if (saveResult)
                    {
                        Debug.WriteLine($"💾 SaveRestoredRankData: Successfully saved {allAccounts.Count} accounts to file");
                    }
                    else
                    {
                        Debug.WriteLine($"❌ SaveRestoredRankData: Failed to save accounts to file");
                    }
                }
                else
                {
                    Debug.WriteLine($"⚠️ SaveRestoredRankData: Account {account.GameName} not found in loaded accounts list");
                    Debug.WriteLine($"⚠️ SaveRestoredRankData: Available accounts: {string.Join(", ", allAccounts.Select(a => a.GameName))}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ SaveRestoredRankData: Exception occurred: {ex.Message}");
                Debug.WriteLine($"❌ SaveRestoredRankData: Stack trace: {ex.StackTrace}");
            }
        }
        
        private static void SaveAccountRankData(Account account)
        {
            try
            {
                Debug.WriteLine($"🔍 SaveAccountRankData: Starting to save rank data for {account.GameName}");
                Debug.WriteLine($"🔍 SaveAccountRankData: CurrentRank = '{account.CurrentRank}', PeakRank = '{account.PeakRank}', RankRating = {account.RankRating}");
                
                // Load all accounts, update the specific account, and save
                var allAccounts = LoadAccounts();
                Debug.WriteLine($"🔍 SaveAccountRankData: Loaded {allAccounts.Count} accounts from file");
                
                var existingAccount = allAccounts.FirstOrDefault(a => 
                    string.Equals(a.GameName?.Trim(), account.GameName?.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(a.TagLine?.Trim(), account.TagLine?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                // Log all accounts for debugging
                Debug.WriteLine($"🔍 SaveAccountRankData: Searching for account '{account.GameName}#{account.TagLine}'");
                foreach (var acc in allAccounts)
                {
                    Debug.WriteLine($"🔍 SaveAccountRankData: Available account: '{acc.GameName}#{acc.TagLine}' (GameName: '{acc.GameName}', TagLine: '{acc.TagLine}')");
                }
                
                if (existingAccount != null)
                {
                    Debug.WriteLine($"🔍 SaveAccountRankData: Found existing account {existingAccount.GameName}, updating rank data");
                    
                    // Update the account with current rank data
                    existingAccount.CurrentRank = account.CurrentRank;
                    existingAccount.PeakRank = account.PeakRank;
                    existingAccount.RankRating = account.RankRating;
                    existingAccount.LastRankUpdate = account.LastRankUpdate;
                    existingAccount.IsRankLoaded = account.IsRankLoaded;
                    existingAccount.IsRankFailed = account.IsRankFailed;
                    existingAccount.LastError = account.LastError;
                    
                    Debug.WriteLine($"🔍 SaveAccountRankData: Updated existing account - CurrentRank = '{existingAccount.CurrentRank}', PeakRank = '{existingAccount.PeakRank}'");
                    
                    // Save all accounts
                    var saveResult = SaveAccounts(allAccounts);
                    if (saveResult)
                    {
                        Debug.WriteLine($"💾 SaveAccountRankData: Successfully saved {allAccounts.Count} accounts to file");
                    }
                    else
                    {
                        Debug.WriteLine($"❌ SaveAccountRankData: Failed to save accounts to file");
                    }
                }
                else
                {
                    Debug.WriteLine($"⚠️ SaveAccountRankData: Account {account.GameName} not found in loaded accounts list");
                    Debug.WriteLine($"⚠️ SaveAccountRankData: Available accounts: {string.Join(", ", allAccounts.Select(a => a.GameName))}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ SaveAccountRankData: Exception occurred: {ex.Message}");
                Debug.WriteLine($"❌ SaveAccountRankData: Stack trace: {ex.StackTrace}");
            }
        }
        
        // Force save rank data for a specific account
        public static bool ForceSaveRankData(Account account)
        {
            try
            {
                Debug.WriteLine($"🔧 ForceSaveRankData: Force saving rank data for {account.GameName}");
                Debug.WriteLine($"🔧 ForceSaveRankData: CurrentRank = '{account.CurrentRank}', PeakRank = '{account.PeakRank}', RankRating = {account.RankRating}");
                
                // Load all accounts
                var allAccounts = LoadAccounts();
                Debug.WriteLine($"🔧 ForceSaveRankData: Loaded {allAccounts.Count} accounts from file");
                
                // Find the account (case-insensitive)
                var existingAccount = allAccounts.FirstOrDefault(a => 
                    string.Equals(a.GameName?.Trim(), account.GameName?.Trim(), StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(a.TagLine?.Trim(), account.TagLine?.Trim(), StringComparison.OrdinalIgnoreCase));
                
                if (existingAccount != null)
                {
                    Debug.WriteLine($"🔧 ForceSaveRankData: Found existing account {existingAccount.GameName}, updating rank data");
                    
                    // Update the account with current rank data
                    existingAccount.CurrentRank = account.CurrentRank;
                    existingAccount.PeakRank = account.PeakRank;
                    existingAccount.RankRating = account.RankRating;
                    existingAccount.LastRankUpdate = account.LastRankUpdate;
                    existingAccount.IsRankLoaded = account.IsRankLoaded;
                    existingAccount.IsRankFailed = account.IsRankFailed;
                    existingAccount.LastError = account.LastError;
                    
                    Debug.WriteLine($"🔧 ForceSaveRankData: Updated existing account - CurrentRank = '{existingAccount.CurrentRank}', PeakRank = '{existingAccount.PeakRank}'");
                    
                    // Save all accounts
                    var saveResult = SaveAccounts(allAccounts);
                    if (saveResult)
                    {
                        Debug.WriteLine($"💾 ForceSaveRankData: Successfully saved {allAccounts.Count} accounts to file");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"❌ ForceSaveRankData: Failed to save accounts to file");
                        return false;
                    }
                }
                else
                {
                    Debug.WriteLine($"⚠️ ForceSaveRankData: Account {account.GameName} not found in loaded accounts list");
                    Debug.WriteLine($"⚠️ ForceSaveRankData: Available accounts: {string.Join(", ", allAccounts.Select(a => $"{a.GameName}#{a.TagLine}"))}");
                    
                    // Try to add the account if not found
                    Debug.WriteLine($"🔧 ForceSaveRankData: Adding new account to list");
                    allAccounts.Add(account);
                    
                    var saveResult = SaveAccounts(allAccounts);
                    if (saveResult)
                    {
                        Debug.WriteLine($"💾 ForceSaveRankData: Successfully saved {allAccounts.Count} accounts (including new account) to file");
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"❌ ForceSaveRankData: Failed to save accounts with new account to file");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ ForceSaveRankData: Exception occurred: {ex.Message}");
                Debug.WriteLine($"❌ ForceSaveRankData: Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        // Debug method to check account loading and saving
        public static void DebugAccountFileOperations()
        {
            try
            {
                Debug.WriteLine($"🔍 DebugAccountFileOperations: ConfigFilePath = {ConfigFilePath}");
                
                // Check if file exists
                if (File.Exists(ConfigFilePath))
                {
                    var fileInfo = new FileInfo(ConfigFilePath);
                    Debug.WriteLine($"🔍 DebugAccountFileOperations: File exists, size: {fileInfo.Length} bytes, last modified: {fileInfo.LastWriteTime}");
                    
                    // Read file content
                    var fileContent = File.ReadAllText(ConfigFilePath);
                    Debug.WriteLine($"🔍 DebugAccountFileOperations: File content length: {fileContent.Length} characters");
                    Debug.WriteLine($"🔍 DebugAccountFileOperations: File content preview: {fileContent.Substring(0, Math.Min(200, fileContent.Length))}...");
                }
                else
                {
                    Debug.WriteLine($"🔍 DebugAccountFileOperations: File does not exist");
                }
                
                // Try to load accounts
                var accounts = LoadAccounts();
                Debug.WriteLine($"🔍 DebugAccountFileOperations: Loaded {accounts.Count} accounts");
                
                foreach (var account in accounts)
                {
                    Debug.WriteLine($"🔍 DebugAccountFileOperations: Account: '{account.GameName}#{account.TagLine}' (GameName: '{account.GameName}', TagLine: '{account.TagLine}')");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ DebugAccountFileOperations: Exception: {ex.Message}");
                Debug.WriteLine($"❌ DebugAccountFileOperations: Stack trace: {ex.StackTrace}");
            }
        }
        
        // Test method to verify rank fetching and saving logic
        public static async Task TestRankFetchAndSave(Account account)
        {
            Debug.WriteLine($"🧪 TestRankFetchAndSave: Starting test for {account.GameName}");
            Debug.WriteLine($"🧪 TestRankFetchAndSave: Initial rank data - CurrentRank: '{account.CurrentRank}', PeakRank: '{account.PeakRank}', RankRating: {account.RankRating}");
            
            // Store initial state
            var initialCurrentRank = account.CurrentRank;
            var initialPeakRank = account.PeakRank;
            var initialRankRating = account.RankRating;
            
            try
            {
                // Call the actual rank update method
                await UpdateAccountRanksAsync(account);
                
                Debug.WriteLine($"🧪 TestRankFetchAndSave: After UpdateAccountRanksAsync - CurrentRank: '{account.CurrentRank}', PeakRank: '{account.PeakRank}', RankRating: {account.RankRating}");
                
                // Check if rank data changed
                if (account.CurrentRank != initialCurrentRank || 
                    account.PeakRank != initialPeakRank || 
                    account.RankRating != initialRankRating)
                {
                    Debug.WriteLine($"✅ TestRankFetchAndSave: Rank data was updated successfully");
                }
                else
                {
                    Debug.WriteLine($"⚠️ TestRankFetchAndSave: Rank data was not updated");
                }
                
                // Verify the data was saved to file
                var savedAccounts = LoadAccounts();
                var savedAccount = savedAccounts.FirstOrDefault(a => 
                    a.GameName == account.GameName && a.TagLine == account.TagLine);
                
                if (savedAccount != null)
                {
                    Debug.WriteLine($"🧪 TestRankFetchAndSave: Found saved account - CurrentRank: '{savedAccount.CurrentRank}', PeakRank: '{savedAccount.PeakRank}', RankRating: {savedAccount.RankRating}");
                    
                    if (savedAccount.CurrentRank == account.CurrentRank &&
                        savedAccount.PeakRank == account.PeakRank &&
                        savedAccount.RankRating == account.RankRating)
                    {
                        Debug.WriteLine($"✅ TestRankFetchAndSave: Rank data was saved to file successfully");
                    }
                    else
                    {
                        Debug.WriteLine($"❌ TestRankFetchAndSave: Rank data was NOT saved to file correctly");
                        Debug.WriteLine($"❌ TestRankFetchAndSave: Expected - CurrentRank: '{account.CurrentRank}', PeakRank: '{account.PeakRank}', RankRating: {account.RankRating}");
                        Debug.WriteLine($"❌ TestRankFetchAndSave: Actual   - CurrentRank: '{savedAccount.CurrentRank}', PeakRank: '{savedAccount.PeakRank}', RankRating: {savedAccount.RankRating}");
                    }
                }
                else
                {
                    Debug.WriteLine($"❌ TestRankFetchAndSave: Could not find saved account in file");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ TestRankFetchAndSave: Exception occurred: {ex.Message}");
                Debug.WriteLine($"❌ TestRankFetchAndSave: Stack trace: {ex.StackTrace}");
            }
        }
    }
} 