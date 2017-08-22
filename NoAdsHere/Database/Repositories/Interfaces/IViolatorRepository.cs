using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using NoAdsHere.Database.Entities.Guild;

namespace NoAdsHere.Database.Repositories.Interfaces
{
    public interface IViolatorRepository : IRepository<Violator>
    {
        Violator Get(IGuildUser user);

        Violator Get(IGuild guild, IUser user);

        Violator Get(ulong guildId, ulong userId);

        Task<Violator> GetAsync(IGuildUser user);

        Task<Violator> GetAsync(IGuild guild, IUser user);

        Task<Violator> GetAsync(ulong guildId, ulong userId);

        Violator GetOrCreate(IGuildUser user);

        Violator GetOrCreate(IGuild guild, IUser user);

        Violator GetOrCreate(ulong guildId, ulong userId);

        Task<Violator> GetOrCreateAsync(IGuildUser user);

        Task<Violator> GetOrCreateAsync(IGuild guild, IUser user);

        Task<Violator> GetOrCreateAsync(ulong guildId, ulong userId);

        IEnumerable<Violator> GetAll(IGuild guild);

        IEnumerable<Violator> GetAll(ulong guildId);

        Task<IEnumerable<Violator>> GetAllAsync(IGuild guild);

        Task<IEnumerable<Violator>> GetAllAsync(ulong guildId);
    }
}