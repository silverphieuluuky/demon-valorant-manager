using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.Models;
using RiotAutoLogin.Services;
using RiotAutoLogin.ViewModels;
using System.Windows;
using System.Linq;

namespace RiotAutoLogin.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private bool _isLoggingIn = false;
        // Thêm biến lưu trạng thái sort hiện tại
        private string _currentSortType = "None";
        public string CurrentSortType
        {
            get => _currentSortType;
            set => SetProperty(ref _currentSortType, value);
        }

        public ObservableCollection<Account> LoginAccounts { get; } = new ObservableCollection<Account>();

        public LoginViewModel(ILogger logger) : base(logger)
        {
            // Initialize commands - Use Task.Run to avoid UI blocking
            LoginCommand = new RelayCommand<Account>(async (account) => 
            {
                if (account != null)
                {
                    await Task.Run(async () => await LoginAsync(account));
                }
            });
            FetchAllRanksCommand = new RelayCommand(async () => await FetchAllRanksAsync());
            TestParallelFetchCommand = new RelayCommand(async () => await TestParallelFetch());
            GetPerformanceStatsCommand = new RelayCommand(() => GetFetchPerformanceStats());
            ApplySortCommand = new RelayCommand<string>(ApplySort);
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand FetchAllRanksCommand { get; }
        public ICommand TestParallelFetchCommand { get; }
        public ICommand GetPerformanceStatsCommand { get; }
        public ICommand ApplySortCommand { get; }

        private async Task LoginAsync(Account? account)
        {
            if (account == null) return;

            try
            {
                // Update UI on main thread
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoggingIn = true;
                    LogInformation($"Starting login process for account: {account.GameName}");
                });

                // Decrypt password and call the actual auto login service on background thread
                string decryptedPassword = EncryptionService.DecryptString(account.EncryptedPassword);
                var riotService = new RiotClientAutomationService(LoggingService.GetLogger<RiotClientAutomationService>());
                await riotService.LaunchAndLoginAsync(account.AccountName, decryptedPassword);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LogInformation($"Login successful for account: {account.GameName}");
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    LogError(ex, $"Login failed for account: {account.GameName}");
                });
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoggingIn = false;
                });
            }
        }

        private async Task FetchAllRanksAsync()
        {
            try
            {
                LogInformation($"🚀 Starting to fetch all ranks for {LoginAccounts.Count} accounts in PARALLEL");
                
                // Clear old rank data for all accounts first
                foreach (var account in LoginAccounts)
                {
                    account.CurrentRank = string.Empty;
                    account.PeakRank = string.Empty;
                    account.IsRankLoaded = false;
                    account.IsRankFailed = false;
                    account.LastError = string.Empty;
                    account.IsRankLoading = true;
                }

                // Check if API key is available
                var apiKey = ApiKeyManager.GetHenrikDevApiKey();
                if (string.IsNullOrEmpty(apiKey))
                {
                    LogWarning("❌ No API key configured - cannot fetch ranks");
                    foreach (var account in LoginAccounts)
                    {
                        account.IsRankLoaded = false;
                        account.IsRankFailed = true;
                        account.LastError = "No API key configured";
                        account.IsRankLoading = false;
                    }
                    return;
                }

                LogInformation("🔑 API key found, creating parallel fetch tasks...");

                // Create tasks for all accounts to fetch ranks in parallel
                var fetchTasks = LoginAccounts.Select(async account =>
                {
                    try
                    {
                        LogInformation($"📡 Starting rank fetch for {account.GameName}#{account.TagLine}");
                        
                        // Use account region or default server if not set
                        var region = !string.IsNullOrEmpty(account.Region) ? account.Region : ServerSettingsService.GetDefaultServer();
                        
                        var henrikDevService = new HenrikDevService();
                        var profile = await henrikDevService.GetPlayerProfileAsync(
                            account.GameName, 
                            account.TagLine, 
                            region, 
                            apiKey,
                            maxRetries: 3
                        );
                        
                        if (profile != null && !string.IsNullOrEmpty(profile.CurrentRank) && profile.CurrentRank != "Unknown")
                        {
                            // Nếu là Unrated, Unranked, Unknown thì không set IsRankLoaded = true
                            var rank = profile.CurrentRank.Trim().ToLower();
                            if (rank != "unrated" && rank != "unranked" && rank != "unknown")
                            {
                                account.CurrentRank = profile.CurrentRank;
                                account.PeakRank = profile.PeakRank;
                                account.RankRating = profile.RankRating;
                                account.LastRankUpdate = DateTime.UtcNow;
                                account.LastError = string.Empty;
                                account.IsRankLoaded = true;
                                account.IsRankFailed = false;
                                LogInformation($"✅ Rank fetched for {account.GameName}#{account.TagLine}: {profile.CurrentRank} (MMR: {profile.RankRating})");
                            }
                            else
                            {
                                account.CurrentRank = profile.CurrentRank;
                                account.PeakRank = profile.PeakRank;
                                account.RankRating = profile.RankRating;
                                account.LastRankUpdate = DateTime.UtcNow;
                                account.IsRankLoaded = false;
                                account.IsRankFailed = false;
                                account.LastError = string.Empty;
                                LogInformation($"⚠️ No rank data for {account.GameName}#{account.TagLine}, show as unranked");
                            }
                        }
                        else
                        {
                            // Don't set any rank - leave it empty to show no rank
                            account.IsRankLoaded = false;
                            account.IsRankFailed = true;
                            account.LastError = "No rank data available";
                            account.LastRankUpdate = DateTime.UtcNow;
                            LogInformation($"❌ No rank data for {account.GameName}#{account.TagLine}, no rank displayed");
                        }
                    }
                    catch (Exception ex)
                    {
                        account.CurrentRank = "Unranked";
                        account.PeakRank = "Unranked";
                        account.IsRankFailed = true;
                        account.LastError = ex.Message;
                        account.LastRankUpdate = DateTime.UtcNow;
                        LogError(ex, $"💥 Failed to fetch rank for {account.GameName}#{account.TagLine}, set to Unranked: {ex.Message}");
                    }
                    finally
                    {
                        account.IsRankLoading = false;
                    }
                });

                // Execute all fetch tasks in parallel
                LogInformation($"⚡ Executing {fetchTasks.Count()} rank fetch tasks in parallel...");
                var startTime = DateTime.UtcNow;
                
                await Task.WhenAll(fetchTasks);
                
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;
                
                LogInformation($"🎉 Completed fetching all ranks in parallel! Duration: {duration.TotalSeconds:F1} seconds");

                // Apply sorting after fetching ranks
                ApplySort(CurrentSortType);

                // Persist all accounts after fetching ranks so data is available after app restart
                try
                {
                    AccountService.SaveAccounts(LoginAccounts.ToList());
                    LogInformation($"💾 Saved {LoginAccounts.Count} accounts with updated ranks");
                }
                catch (Exception ex)
                {
                    LogError(ex, "Error saving accounts after fetching ranks");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error fetching all ranks in parallel");
            }
        }

        private void ApplySort(string? sortType)
        {
            try
            {
                LogInformation($"Applying sort: {sortType ?? "None"}");
                // Lưu lại sort type hiện tại
                CurrentSortType = sortType ?? "None";
                var sortedAccounts = sortType switch
                {
                    "Ascending" => LoginAccounts.OrderBy(a => GetRankValue(a.CurrentRank)).ToList(),
                    "Descending" => LoginAccounts.OrderByDescending(a => GetRankValue(a.CurrentRank)).ToList(),
                    _ => LoginAccounts.ToList() // No sort
                };
                LoginAccounts.Clear();
                foreach (var account in sortedAccounts)
                {
                    LoginAccounts.Add(account);
                }
                LogInformation($"Sort applied successfully. {LoginAccounts.Count} accounts sorted.");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error applying sort");
            }
        }

        private int GetRankValue(string rank)
        {
            if (string.IsNullOrEmpty(rank) || rank.ToLower() == "unranked" || rank.ToLower() == "unrated")
                return 0;

            return rank.ToLower() switch
            {
                var r when r.Contains("iron") => 1,
                var r when r.Contains("bronze") => 2,
                var r when r.Contains("silver") => 3,
                var r when r.Contains("gold") => 4,
                var r when r.Contains("platinum") => 5,
                var r when r.Contains("diamond") => 6,
                var r when r.Contains("ascendant") => 7,
                var r when r.Contains("immortal") => 8,
                var r when r.Contains("radiant") => 9,
                _ => 0
            };
        }

        /// <summary>
        /// Test method to verify parallel rank fetching is working
        /// </summary>
        public async Task TestParallelFetch()
        {
            try
            {
                LogInformation("🧪 Testing parallel rank fetching...");
                
                if (LoginAccounts.Count == 0)
                {
                    LogWarning("No accounts to test with");
                    return;
                }

                // Create a small test set
                var testAccounts = LoginAccounts.Take(3).ToList();
                LogInformation($"Testing with {testAccounts.Count} accounts: {string.Join(", ", testAccounts.Select(a => $"{a.GameName}#{a.TagLine}"))}");

                var startTime = DateTime.UtcNow;
                
                // Test parallel execution
                var testTasks = testAccounts.Select(async account =>
                {
                    await Task.Delay(1000); // Simulate API call
                    LogInformation($"Test task completed for {account.GameName}#{account.TagLine}");
                });

                await Task.WhenAll(testTasks);
                
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;
                
                LogInformation($"🧪 Parallel test completed! Duration: {duration.TotalSeconds:F1} seconds (should be ~1 second for 3 accounts)");
            }
            catch (Exception ex)
            {
                LogError(ex, "Error during parallel fetch test");
            }
        }

        /// <summary>
        /// Get performance statistics for rank fetching
        /// </summary>
        public string GetFetchPerformanceStats()
        {
            try
            {
                var totalAccounts = LoginAccounts.Count;
                var loadedAccounts = LoginAccounts.Count(a => a.IsRankLoaded);
                var failedAccounts = LoginAccounts.Count(a => a.IsRankFailed);
                var loadingAccounts = LoginAccounts.Count(a => a.IsRankLoading);
                
                var stats = $"📊 Rank Fetch Performance Stats:\n" +
                           $"Total Accounts: {totalAccounts}\n" +
                           $"Successfully Loaded: {loadedAccounts}\n" +
                           $"Failed: {failedAccounts}\n" +
                           $"Currently Loading: {loadingAccounts}\n" +
                           $"Success Rate: {(totalAccounts > 0 ? (loadedAccounts * 100.0 / totalAccounts) : 0):F1}%";
                
                LogInformation(stats);
                return stats;
            }
            catch (Exception ex)
            {
                LogError(ex, "Error getting performance stats");
                return "Error getting performance stats";
            }
        }
    }
} 