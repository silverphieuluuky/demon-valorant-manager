using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.Interfaces;
using RiotAutoLogin.Models;

namespace RiotAutoLogin.Services
{
    public class AccountCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountCacheService> _logger;
        
        private const string ACCOUNTS_CACHE_KEY = "accounts_cache";
        private const string ACCOUNT_CACHE_PREFIX = "account_";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

        public AccountCacheService(IMemoryCache cache, IAccountService accountService, ILogger<AccountCacheService> logger)
        {
            _cache = cache;
            _accountService = accountService;
            _logger = logger;
        }

        /// <summary>
        /// Get all accounts with caching
        /// </summary>
        public async Task<List<Account>> GetAccountsAsync()
        {
            try
            {
                // Try to get from cache first
                if (_cache.TryGetValue(ACCOUNTS_CACHE_KEY, out List<Account>? cachedAccounts) && cachedAccounts != null)
                {
                    _logger.LogDebug("Retrieved {Count} accounts from cache", cachedAccounts.Count);
                    return cachedAccounts;
                }

                // If not in cache, load from service
                _logger.LogDebug("Cache miss, loading accounts from service");
                var accounts = await _accountService.LoadAccountsAsync();
                
                // Store in cache
                _cache.Set(ACCOUNTS_CACHE_KEY, accounts, CACHE_DURATION);
                _logger.LogInformation("Cached {Count} accounts for {Duration} minutes", accounts.Count, CACHE_DURATION.TotalMinutes);
                
                return accounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounts from cache");
                // Fallback to direct service call
                return await _accountService.LoadAccountsAsync();
            }
        }

        /// <summary>
        /// Get account by GameName and TagLine with caching
        /// </summary>
        public async Task<Account?> GetAccountAsync(string gameName, string tagLine)
        {
            try
            {
                var cacheKey = $"{ACCOUNT_CACHE_PREFIX}{gameName}_{tagLine}";
                
                // Try to get from cache first
                if (_cache.TryGetValue(cacheKey, out Account? cachedAccount) && cachedAccount != null)
                {
                    _logger.LogDebug("Retrieved account {GameName}#{TagLine} from cache", gameName, tagLine);
                    return cachedAccount;
                }

                // If not in cache, get from all accounts
                var accounts = await GetAccountsAsync();
                var account = accounts.FirstOrDefault(a => 
                    string.Equals(a.GameName, gameName, StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(a.TagLine, tagLine, StringComparison.OrdinalIgnoreCase));
                
                if (account != null)
                {
                    // Store individual account in cache
                    _cache.Set(cacheKey, account, CACHE_DURATION);
                    _logger.LogDebug("Cached individual account {GameName}#{TagLine}", gameName, tagLine);
                }
                
                return account;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account {GameName}#{TagLine} from cache", gameName, tagLine);
                return null;
            }
        }

        /// <summary>
        /// Save accounts and update cache
        /// </summary>
        public async Task<bool> SaveAccountsAsync(List<Account> accounts)
        {
            try
            {
                // Save to service
                var success = await _accountService.SaveAccountsAsync(accounts);
                
                if (success)
                {
                    // Update cache
                    _cache.Set(ACCOUNTS_CACHE_KEY, accounts, CACHE_DURATION);
                    
                    // Update individual account caches
                    foreach (var account in accounts)
                    {
                        var cacheKey = $"{ACCOUNT_CACHE_PREFIX}{account.GameName}_{account.TagLine}";
                        _cache.Set(cacheKey, account, CACHE_DURATION);
                    }
                    
                    _logger.LogInformation("Saved and cached {Count} accounts", accounts.Count);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving accounts to cache");
                return false;
            }
        }

        /// <summary>
        /// Update single account and refresh cache
        /// </summary>
        public async Task<bool> UpdateAccountAsync(Account account)
        {
            try
            {
                // Get all accounts
                var accounts = await GetAccountsAsync();
                
                // Find and update the account
                var existingAccount = accounts.FirstOrDefault(a => 
                    string.Equals(a.GameName, account.GameName, StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(a.TagLine, account.TagLine, StringComparison.OrdinalIgnoreCase));
                
                if (existingAccount != null)
                {
                    // Update properties
                    existingAccount.CurrentRank = account.CurrentRank;
                    existingAccount.PeakRank = account.PeakRank;
                    existingAccount.RankRating = account.RankRating;
                    existingAccount.LastRankUpdate = account.LastRankUpdate;
                    existingAccount.IsRankLoaded = account.IsRankLoaded;
                    existingAccount.IsRankFailed = account.IsRankFailed;
                    existingAccount.LastError = account.LastError;
                    existingAccount.Region = account.Region;
                    existingAccount.AvatarPath = account.AvatarPath;
                }
                else
                {
                    accounts.Add(account);
                }
                
                // Save and update cache
                return await SaveAccountsAsync(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account {GameName}#{TagLine}", account.GameName, account.TagLine);
                return false;
            }
        }

        /// <summary>
        /// Remove account from cache and storage
        /// </summary>
        public async Task<bool> RemoveAccountAsync(string gameName, string tagLine)
        {
            try
            {
                var accounts = await GetAccountsAsync();
                var accountToRemove = accounts.FirstOrDefault(a => 
                    string.Equals(a.GameName, gameName, StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(a.TagLine, tagLine, StringComparison.OrdinalIgnoreCase));
                
                if (accountToRemove != null)
                {
                    accounts.Remove(accountToRemove);
                    
                    // Remove from cache
                    var cacheKey = $"{ACCOUNT_CACHE_PREFIX}{gameName}_{tagLine}";
                    _cache.Remove(cacheKey);
                    
                    // Save updated list
                    return await SaveAccountsAsync(accounts);
                }
                
                return true; // Account not found, consider it removed
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing account {GameName}#{TagLine}", gameName, tagLine);
                return false;
            }
        }

        /// <summary>
        /// Clear all cache
        /// </summary>
        public void ClearCache()
        {
            try
            {
                _cache.Remove(ACCOUNTS_CACHE_KEY);
                
                // Note: We can't easily remove all individual account caches
                // but they will expire automatically
                
                _logger.LogInformation("Cleared accounts cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
            }
        }

        /// <summary>
        /// Get accounts by region with caching
        /// </summary>
        public async Task<List<Account>> GetAccountsByRegionAsync(string region)
        {
            try
            {
                var accounts = await GetAccountsAsync();
                return accounts.Where(a => string.Equals(a.Region, region, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounts by region {Region}", region);
                return new List<Account>();
            }
        }

        /// <summary>
        /// Get accounts with valid ranks
        /// </summary>
        public async Task<List<Account>> GetAccountsWithValidRanksAsync()
        {
            try
            {
                var accounts = await GetAccountsAsync();
                return accounts.Where(a => a.IsRankLoaded && !a.IsRankFailed).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounts with valid ranks");
                return new List<Account>();
            }
        }

        /// <summary>
        /// Get accounts that need rank update
        /// </summary>
        public async Task<List<Account>> GetAccountsNeedingRankUpdateAsync()
        {
            try
            {
                var accounts = await GetAccountsAsync();
                var cutoffTime = DateTime.UtcNow.AddHours(-6); // 6 hours ago
                
                return accounts.Where(a => 
                    a.LastRankUpdate < cutoffTime || 
                    a.IsRankFailed || 
                    string.IsNullOrEmpty(a.CurrentRank)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accounts needing rank update");
                return new List<Account>();
            }
        }
    }
}
