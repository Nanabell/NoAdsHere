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
    public class StatisticRepository : Repository<Statistic>, IStatisticRepository
    {
        public StatisticRepository(NoAdsHereContext context) : base(context)
        {
        }

        public Statistic Get(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return Get(guild.Id);
        }

        public Statistic Get(ulong guildId)
            => Context.Statistics.FirstOrDefault(statistic => statistic.GuildId == guildId);

        public async Task<Statistic> GetAsync(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetAsync(guild.Id);
        }

        public async Task<Statistic> GetAsync(ulong guildId)
            => await Context.Statistics.FirstOrDefaultAsync(statistic => statistic.GuildId == guildId);

        public Statistic GetOrCreate(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return GetOrCreate(guild.Id);
        }

        public Statistic GetOrCreate(ulong guildId)
        {
            var statistic = Get(guildId);
            if (statistic != null)
                return statistic;

            statistic = new Statistic(guildId);
            Context.Statistics.Add(statistic);
            Context.SaveChanges();
            return Get(guildId);
        }

        public async Task<Statistic> GetOrCreateAsync(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetOrCreateAsync(guild.Id);
        }

        public async Task<Statistic> GetOrCreateAsync(ulong guildId)
        {
            var statistic = await GetAsync(guildId);
            if (statistic != null)
                return statistic;

            statistic = new Statistic(guildId);
            await Context.Statistics.AddAsync(statistic);
            await Context.SaveChangesAsync();
            return await GetAsync(guildId);
        }

        public Statistic GetGlobal()
        {
            var globalStats = GetAll();
            var stats = globalStats as IList<Statistic> ?? globalStats.ToList();
            return new Statistic
            {
                Blocks = (uint)stats.Sum(statistic => statistic.Blocks),
                Warns = (uint)stats.Sum(statistic => statistic.Warns),
                Kicks = (uint)stats.Sum(statistic => statistic.Kicks),
                Bans = (uint)stats.Sum(statistic => statistic.Bans),
            };
        }

        public async Task<Statistic> GetGlobalAsync()
        {
            var globalStats = await GetAllAsync();
            var stats = globalStats as IList<Statistic> ?? globalStats.ToList();
            return new Statistic
            {
                Blocks = (uint)stats.Sum(statistic => statistic.Blocks),
                Warns = (uint)stats.Sum(statistic => statistic.Warns),
                Kicks = (uint)stats.Sum(statistic => statistic.Kicks),
                Bans = (uint)stats.Sum(statistic => statistic.Bans),
            };
        }
    }
}