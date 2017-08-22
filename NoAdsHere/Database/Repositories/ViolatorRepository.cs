using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.Repositories.Interfaces;

namespace NoAdsHere.Database.Repositories
{
    public class ViolatorRepository : Repository<Violator>, IViolatorRepository
    {
        public ViolatorRepository(NoAdsHereContext context) : base(context)
        {
        }

        public Violator Get(IGuildUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Get(user.Guild, user);
        }

        public Violator Get(IGuild guild, IUser user)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Get(guild.Id, user.Id);
        }

        public Violator Get(ulong guildId, ulong userId)
            => Context.Violators.FirstOrDefault(violator => violator.GuildId == guildId && violator.UserId == userId);

        public async Task<Violator> GetAsync(IGuildUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return await GetAsync(user.Guild, user);
        }

        public async Task<Violator> GetAsync(IGuild guild, IUser user)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (user == null) throw new ArgumentNullException(nameof(user));
            return await GetAsync(guild.Id, user.Id);
        }

        public async Task<Violator> GetAsync(ulong guildId, ulong userId)
            => await Context.Violators.FirstOrDefaultAsync(violator => violator.GuildId == guildId && violator.UserId == userId);

        public Violator GetOrCreate(IGuildUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return GetOrCreate(user.Guild, user);
        }

        public Violator GetOrCreate(IGuild guild, IUser user)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (user == null) throw new ArgumentNullException(nameof(user));
            return GetOrCreate(guild.Id, user.Id);
        }

        public Violator GetOrCreate(ulong guildId, ulong userId)
        {
            var violator = Get(guildId, userId);
            if (violator != null)
                return violator;

            violator = new Violator
            {
                GuildId = guildId,
                UserId = userId
            };
            Context.Violators.Add(violator);
            Context.SaveChanges();
            return Get(guildId, userId);
        }

        public async Task<Violator> GetOrCreateAsync(IGuildUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return await GetOrCreateAsync(user.Guild, user);
        }

        public async Task<Violator> GetOrCreateAsync(IGuild guild, IUser user)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (user == null) throw new ArgumentNullException(nameof(user));
            return await GetOrCreateAsync(guild.Id, user.Id);
        }

        public async Task<Violator> GetOrCreateAsync(ulong guildId, ulong userId)
        {
            var violator = await GetAsync(guildId, userId);
            if (violator != null)
                return violator;

            violator = new Violator
            {
                GuildId = guildId,
                UserId = userId
            };
            await Context.Violators.AddAsync(violator);
            await Context.SaveChangesAsync();
            return await GetAsync(guildId, userId);
        }

        public IEnumerable<Violator> GetAll(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return GetAll(guild.Id);
        }

        public IEnumerable<Violator> GetAll(ulong guildId)
            => Context.Violators.Where(violator => violator.GuildId == guildId);

        public async Task<IEnumerable<Violator>> GetAllAsync(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetAllAsync(guild.Id);
        }

        public async Task<IEnumerable<Violator>> GetAllAsync(ulong guildId)
            => await Context.Violators.Where(violator => violator.GuildId == guildId).ToListAsync();
    }
}