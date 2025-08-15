using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RiotAutoLogin.Interfaces;
using RiotAutoLogin.Models;

namespace RiotAutoLogin.Services
{
    public class OptimizedAccountService : IAccountService
    {
        private readonly ILogger<OptimizedAccountService> _logger;
        private readonly AsyncAccountService _asyncService;

        public OptimizedAccountService(ILogger<OptimizedAccountService> logger)
        {
            _logger = logger;
            _asyncService = new AsyncAccountService(LoggingService.GetLogger<AsyncAccountService>());
        }

        // Synchronous methods (delegate to original AccountService for backward compatibility)
        public List<Account> LoadAccounts()
        {
            return AccountService.LoadAccounts();
        }

        public List<Account> LoadAccounts(string configPath)
        {
            return AccountService.LoadAccounts(configPath);
        }

        public bool SaveAccounts(List<Account> accounts)
        {
            return AccountService.SaveAccounts(accounts);
        }

        public bool SaveAccounts(List<Account> accounts, string configPath)
        {
            return AccountService.SaveAccounts(accounts, configPath);
        }

        public bool ForceSaveRankData(Account account)
        {
            return AccountService.ForceSaveRankData(account);
        }

        // Asynchronous methods (delegate to AsyncAccountService)
        public async Task<List<Account>> LoadAccountsAsync()
        {
            return await _asyncService.LoadAccountsAsync();
        }

        public async Task<List<Account>> LoadAccountsAsync(string configPath)
        {
            return await _asyncService.LoadAccountsAsync(configPath);
        }

        public async Task<bool> SaveAccountsAsync(List<Account> accounts)
        {
            return await _asyncService.SaveAccountsAsync(accounts);
        }

        public async Task<bool> SaveAccountsAsync(List<Account> accounts, string configPath)
        {
            return await _asyncService.SaveAccountsAsync(accounts, configPath);
        }

        public async Task<bool> ForceSaveRankDataAsync(Account account)
        {
            return await _asyncService.ForceSaveRankDataAsync(account);
        }

        public async Task UpdateAllAccountsAsync(List<Account> accounts)
        {
            await _asyncService.UpdateAllAccountsAsync(accounts);
        }

        public async Task UpdateAccountRanksAsync(Account account)
        {
            await _asyncService.UpdateAccountRanksAsync(account);
        }
    }
}
