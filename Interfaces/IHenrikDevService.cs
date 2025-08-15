using System.Threading.Tasks;
using RiotAutoLogin.Models;

namespace RiotAutoLogin.Interfaces
{
    public interface IHenrikDevService
    {
        Task<ValorantProfile?> GetPlayerProfileAsync(string username, string tag, string region, string apiKey, int maxRetries = 3);
    }
}
