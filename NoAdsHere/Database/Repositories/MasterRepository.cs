using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.EntityFrameworkCore;
using NoAdsHere.Database.Entities.Global;
using NoAdsHere.Database.Repositories.Interfaces;

namespace NoAdsHere.Database.Repositories
{
    public class MasterRepository : Repository<Master>, IMasterRepository
    {
        public MasterRepository(NoAdsHereContext context) : base(context)
        {
        }

        public Master Get(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Get(user.Id);
        }

        public Master Get(ulong userId)
            => Context.Masters.FirstOrDefault(master => master.UserId == userId);

        public async Task<Master> GetAsync(IUser user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return await GetAsync(user.Id);
        }

        public async Task<Master> GetAsync(ulong userId)
            => await Context.Masters.FirstOrDefaultAsync(master => master.UserId == userId);
    }
}