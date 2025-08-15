using System.Collections.Generic;
using System.Threading.Tasks;
using RiotAutoLogin.Models;

namespace RiotAutoLogin.Interfaces
{
    public interface IAccountService
    {
        // Synchronous methods (for backward compatibility)
        List<Account> LoadAccounts();
        List<Account> LoadAccounts(string configPath);
        bool SaveAccounts(List<Account> accounts);
        bool SaveAccounts(List<Account> accounts, string configPath);
        bool ForceSaveRankData(Account account);
        
        // Asynchronous methods (recommended)
        Task<List<Account>> LoadAccountsAsync();
        Task<List<Account>> LoadAccountsAsync(string configPath);
        Task<bool> SaveAccountsAsync(List<Account> accounts);
        Task<bool> SaveAccountsAsync(List<Account> accounts, string configPath);
        Task<bool> ForceSaveRankDataAsync(Account account);
        
        // Rank update methods
        Task UpdateAllAccountsAsync(List<Account> accounts);
        Task UpdateAccountRanksAsync(Account account);
    }
}
