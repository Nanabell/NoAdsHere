using Discord;
using NoAdsHere.Database.Entities.Guild;
using System.Threading.Tasks;

namespace NoAdsHere.Database.Repositories.Interfaces
{
    public interface ISettingsRepository : IRepository<Settings>
    {
        Settings Get(IGuild guild);

        Settings Get(ulong guildId);

        Task<Settings> GetAsync(IGuild guild);

        Task<Settings> GetAsync(ulong guildId);

        Task<Settings> GetOrCreateAsync(IGuild guild);

        Task<Settings> GetOrCreateAsync(ulong guildId);
    }
}