using Discord;
using Microsoft.EntityFrameworkCore;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.Repositories.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace NoAdsHere.Database.Repositories
{
    public class SettingsRepository : Repository<Settings>, ISettingsRepository
    {
        public SettingsRepository(NoAdsHereContext context) : base(context)
        {
        }

        public Settings Get(IGuild guild)
            => Get(guild?.Id ?? 0);

        public Settings Get(ulong guildId)
            => Context.Settings.FirstOrDefault(setting => setting.GuildId == guildId);

        public async Task<Settings> GetAsync(IGuild guild)
            => await GetAsync(guild?.Id ?? 0);

        public async Task<Settings> GetAsync(ulong guildId)
            => await Context.Settings.FirstOrDefaultAsync(setting => setting.GuildId == guildId);

        public async Task<Settings> GetOrCreateAsync(IGuild guild)
            => await GetOrCreateAsync(guild?.Id ?? 0);

        public async Task<Settings> GetOrCreateAsync(ulong guildId)
        {
            var settings = await GetAsync(guildId);
            if (settings != null)
                return settings;
            await AddAsync(new Settings { GuildId = guildId });
            Context.SaveChanges();
            return await GetAsync(guildId);
        }
    }
}