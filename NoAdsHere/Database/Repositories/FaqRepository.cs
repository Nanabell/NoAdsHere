using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MoreLinq;
using NoAdsHere.Common;
using NoAdsHere.Database.Entities.Guild;
using NoAdsHere.Database.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NoAdsHere.Database.Repositories
{
    public class FaqRepository : Repository<Faq>, IFaqRepository
    {
        public FaqRepository(NoAdsHereContext context) : base(context)
        {
        }

        public Faq Get(IGuild guild, string name)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return Get(guild.Id, name);
        }

        public Faq Get(ulong guildId, string name)
            => Context.Faqs.FirstOrDefault(faq => faq.GuildId == guildId && faq.Name == name);

        public async Task<Faq> GetAsync(IGuild guild, string name)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetAsync(guild.Id, name);
        }

        public async Task<Faq> GetAsync(ulong guildId, string name)
            => await Context.Faqs.FirstOrDefaultAsync(faq => faq.GuildId == guildId && faq.Name == name);

        public IEnumerable<Faq> GetAll(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return GetAll(guild.Id);
        }

        public IEnumerable<Faq> GetAll(ulong guildId)
            => Context.Faqs.Where(faq => faq.GuildId == guildId).ToList();

        public async Task<IEnumerable<Faq>> GetAllAsync(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetAllAsync(guild.Id);
        }

        public async Task<IEnumerable<Faq>> GetAllAsync(ulong guildId)
            => await Context.Faqs.Where(faq => faq.GuildId == guildId).ToListAsync();

        public Dictionary<Faq, int> GetSimilar(IConfigurationRoot config, IGuild guild, string name)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return GetSimilar(config, guild.Id, name);
        }

        public Dictionary<Faq, int> GetSimilar(IConfigurationRoot config, ulong guildId, string name)
        {
            var faqs = GetAll(guildId);
            var faqDictionary = faqs.ToDictionary(faq => faq, faq => LevenshteinDistance.Compute(name, faq.Name));
            return faqDictionary
                .Where(pair => pair.Value <= Convert.ToInt32(config["MaxLevenshteinDistance"]))
                .OrderBy(pair => pair.Value)
                .ToDictionary();
        }

        public async Task<Dictionary<Faq, int>> GetSimilarAsync(IConfigurationRoot config, IGuild guild, string name)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetSimilarAsync(config, guild.Id, name);
        }

        public async Task<Dictionary<Faq, int>> GetSimilarAsync(IConfigurationRoot config, ulong guildId, string name)
        {
            var faqs = await GetAllAsync(guildId);
            var faqDictionary = faqs.ToDictionary(faq => faq, faq => LevenshteinDistance.Compute(name, faq.Name));
            return faqDictionary
                .Where(pair => pair.Value <= Convert.ToInt32(config["MaxLevenshteinDistance"]))
                .OrderBy(pair => pair.Value)
                .ToDictionary();
        }
    }
}