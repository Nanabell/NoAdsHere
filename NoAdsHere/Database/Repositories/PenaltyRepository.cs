using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using NoAdsHere.Common;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.Repositories.Interfaces;

namespace NoAdsHere.Database.Repositories
{
    public class PenaltyRepository : Repository<Penalty>, IPenaltyRepository
    {
        public PenaltyRepository(NoAdsHereContext context) : base(context)
        {
        }

        public IEnumerable<Penalty> GetAll(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return GetAll(guild.Id);
        }

        public IEnumerable<Penalty> GetAll(ulong guildId)
            => Context.Penalties.Where(penalty => penalty.GuildId == guildId);

        public IEnumerable<Penalty> GetOrCreateAll(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return GetOrCreateAll(guild.Id);
        }

        public IEnumerable<Penalty> GetOrCreateAll(ulong guildId)
        {
            var penalties = GetAll(guildId).ToList();
            if (penalties.Count != 0)
                return penalties;

            AddDefault(guildId);
            return GetAll(guildId);
        }

        public async Task<IEnumerable<Penalty>> GetAllAsync(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetAllAsync(guild.Id);
        }

        public async Task<IEnumerable<Penalty>> GetAllAsync(ulong guildId)
            => await Context.Penalties.Where(penalty => penalty.GuildId == guildId).ToListAsync();

        public async Task<IEnumerable<Penalty>> GetOrCreateAllAsync(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetOrCreateAllAsync(guild.Id);
        }

        public async Task<IEnumerable<Penalty>> GetOrCreateAllAsync(ulong guildId)
        {
            var penalties = (await GetAllAsync(guildId)).ToList();
            if (penalties.Count != 0)
                return penalties;

            AddDefault(guildId);
            return await GetAllAsync(guildId);
        }

        private void AddDefault(ulong guildId)
        {
            var newPenalties = new List<Penalty>
            {
                new Penalty(guildId, PenaltyType.Nothing, 1, autoDelete: true),
                new Penalty(guildId, PenaltyType.Warn, 3, autoDelete: true),
                new Penalty(guildId, PenaltyType.Kick, 5, autoDelete: true),
                new Penalty(guildId, PenaltyType.Ban, 6, autoDelete: true)
            };
            Context.Penalties.AddRange(newPenalties);
            Context.SaveChanges();
        }
    }
}