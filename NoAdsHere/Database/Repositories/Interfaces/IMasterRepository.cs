using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using NoAdsHere.Database.Entities.Global;

namespace NoAdsHere.Database.Repositories.Interfaces
{
    public interface IMasterRepository : IRepository<Master>
    {
        Master Get(IUser user);

        Master Get(ulong userId);

        Task<Master> GetAsync(IUser user);

        Task<Master> GetAsync(ulong userId);
    }
}