using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using NoAdsHere.Database.Entities.Guild;

namespace NoAdsHere.Database.Repositories.Interfaces
{
    public interface IPenaltyRepository : IRepository<Penalty>
    {
        IEnumerable<Penalty> GetAll(IGuild guild);

        IEnumerable<Penalty> GetAll(ulong guildId);

        IEnumerable<Penalty> GetOrCreateAll(IGuild guild);

        IEnumerable<Penalty> GetOrCreateAll(ulong guildId);

        Task<IEnumerable<Penalty>> GetAllAsync(IGuild guild);

        Task<IEnumerable<Penalty>> GetAllAsync(ulong guildId);

        Task<IEnumerable<Penalty>> GetOrCreateAllAsync(IGuild guild);

        Task<IEnumerable<Penalty>> GetOrCreateAllAsync(ulong guildId);
    }
}