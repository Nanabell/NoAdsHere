using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using NoAdsHere.Database.Entities.Guild;

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

        Dictionary<Faq, int> GetSimilar(IGuild guild, string name);

        Dictionary<Faq, int> GetSimilar(ulong guildId, string name);

        Task<Dictionary<Faq, int>> GetSimilarAsync(IGuild guild, string name);

        Task<Dictionary<Faq, int>> GetSimilarAsync(ulong guildId, string name);
    }
}