using System.Threading.Tasks;
using Discord;
using NoAdsHere.Database.Entities.Guild;

namespace NoAdsHere.Database.Repositories.Interfaces
{
    public interface IStatisticRepository : IRepository<Statistic>
    {
        Statistic Get(IGuild guild);

        Statistic Get(ulong guildId);

        Task<Statistic> GetAsync(IGuild guild);

        Task<Statistic> GetAsync(ulong guildId);

        Statistic GetOrCreate(IGuild guild);

        Statistic GetOrCreate(ulong guildId);

        Task<Statistic> GetOrCreateAsync(IGuild guild);

        Task<Statistic> GetOrCreateAsync(ulong guildId);

        Statistic GetGlobal();

        Task<Statistic> GetGlobalAsync();
    }
}