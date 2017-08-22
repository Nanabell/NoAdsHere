using Discord;
using NoAdsHere.Common;
using NoAdsHere.Database.Entities.Guild;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoAdsHere.Database.Repositories.Interfaces
{
    public interface IIgnoreRepository : IRepository<Ignore>
    {
        IEnumerable<Ignore> Get(IGuildUser guildUser);

        IEnumerable<Ignore> Get(IRole role);

        IEnumerable<Ignore> Get(IGuild guild, IUser user);

        IEnumerable<Ignore> Get(ulong guildId, ulong ignoredId);

        IEnumerable<Ignore> GetAll(IGuild guild);

        IEnumerable<Ignore> GetAll(ulong guildId);

        IEnumerable<Ignore> GetAll(IGuild guild, IgnoreType ignoreType);

        IEnumerable<Ignore> GetAll(ulong guildId, IgnoreType ignoreType);

        Task<IEnumerable<Ignore>> GetAllAsync(IGuild guild);

        Task<IEnumerable<Ignore>> GetAllAsync(ulong guildId);

        Task<IEnumerable<Ignore>> GetAllAsync(IGuild guild, IgnoreType ignoreType);

        Task<IEnumerable<Ignore>> GetAllAsync(ulong guildId, IgnoreType ignoreType);
    }
}