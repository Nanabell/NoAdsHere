using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using NoAdsHere.Common;
using NoAdsHere.Database.Entities.Guild;

namespace NoAdsHere.Database.Repositories.Interfaces
{
    public interface IBlockRepository : IRepository<Block>
    {
        Block Get(IGuild guild, BlockType blockType);

        Block Get(ulong guildId, BlockType blockType);

        Task<Block> GetAsync(IGuild guild, BlockType blockType);

        Task<Block> GetAsync(ulong guildId, BlockType blockType);

        IEnumerable<Block> GetAll(IGuild guild);

        IEnumerable<Block> GetAll(ulong guildId);

        Task<IEnumerable<Block>> GetAllAsync(IGuild guild);

        Task<IEnumerable<Block>> GetAllAsync(ulong guildId);
    }
}