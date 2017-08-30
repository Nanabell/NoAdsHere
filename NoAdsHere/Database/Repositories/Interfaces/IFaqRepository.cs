using Discord;
using Microsoft.Extensions.Configuration;
using NoAdsHere.Database.Entities.Guild;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoAdsHere.Database.Repositories.Interfaces
{
    public interface IFaqRepository : IRepository<Faq>
    {
        Faq Get(IGuild guild, string name);

        Faq Get(ulong guildId, string name);

        Task<Faq> GetAsync(IGuild guild, string name);

        Task<Faq> GetAsync(ulong guildId, string name);

        IEnumerable<Faq> GetAll(IGuild guild);

        IEnumerable<Faq> GetAll(ulong guildId);

        Task<IEnumerable<Faq>> GetAllAsync(IGuild guild);

        Task<IEnumerable<Faq>> GetAllAsync(ulong guildId);

        Dictionary<Faq, int> GetSimilar(IConfigurationRoot config, IGuild guild, string name);

        Dictionary<Faq, int> GetSimilar(IConfigurationRoot config, ulong guildId, string name);

        Task<Dictionary<Faq, int>> GetSimilarAsync(IConfigurationRoot config, IGuild guild, string name);

        Task<Dictionary<Faq, int>> GetSimilarAsync(IConfigurationRoot config, ulong guildId, string name);
    }
}