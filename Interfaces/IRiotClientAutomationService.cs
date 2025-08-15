using RiotAutoLogin.Models;
using System.Threading.Tasks;

namespace RiotAutoLogin.Interfaces
{
    public interface IRiotClientAutomationService
    {
        Task<bool> LoginAccountAsync(Account account);
        Task LaunchAndLoginAsync(string username, string password);
        Task<bool> IsRiotClientRunningAsync();
        Task<bool> KillRiotClientAsync();
    }
}
