using Discord;
using Microsoft.EntityFrameworkCore;
using NoAdsHere.Common;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NoAdsHere.Database.Repositories
{
    public class IgnoreRepository : Repository<Ignore>, IIgnoreRepository
    {
        public IgnoreRepository(NoAdsHereContext context) : base(context)
        {
        }

        public IEnumerable<Ignore> Get(IGuildUser guildUser)
        {
            if (guildUser == null) throw new ArgumentNullException(nameof(guildUser));
            return Get(guildUser.Guild.Id, guildUser.Id);
        }

        public IEnumerable<Ignore> Get(IRole role)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            return Get(role.Guild.Id, role.Id);
        }

        public IEnumerable<Ignore> Get(IGuild guild, IUser user)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Get(guild.Id, user.Id);
        }

        public IEnumerable<Ignore> Get(ulong guildId, ulong ignoredId)
            => Find(ignore => ignore.GuildId == guildId && ignore.IgnoredId == ignoredId);

        public IEnumerable<Ignore> GetAll(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return GetAll(guild.Id);
        }

        public IEnumerable<Ignore> GetAll(ulong guildId)
            => Context.Ignores.Where(ignore => ignore.GuildId == guildId);

        public IEnumerable<Ignore> GetAll(IGuild guild, IgnoreType ignoreType)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return GetAll(guild.Id, ignoreType);
        }

        public IEnumerable<Ignore> GetAll(ulong guildId, IgnoreType ignoreType)
            => Context.Ignores.Where(ignore => ignore.GuildId == guildId && ignore.IgnoreType == ignoreType);

        public async Task<IEnumerable<Ignore>> GetAllAsync(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetAllAsync(guild.Id);
        }

        public async Task<IEnumerable<Ignore>> GetAllAsync(ulong guildId)
            => await Context.Ignores.Where(ignore => ignore.GuildId == guildId).ToListAsync();

        public async Task<IEnumerable<Ignore>> GetAllAsync(IGuild guild, IgnoreType ignoreType)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetAllAsync(guild.Id, ignoreType);
        }

        public async Task<IEnumerable<Ignore>> GetAllAsync(ulong guildId, IgnoreType ignoreType)
            => await Context.Ignores.Where(ignore => ignore.GuildId == guildId && ignore.IgnoreType == ignoreType).ToListAsync();
    }
}