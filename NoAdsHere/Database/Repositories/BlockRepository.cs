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
    public class BlockRepository : Repository<Block>, IBlockRepository
    {
        public BlockRepository(NoAdsHereContext context) : base(context)
        {
        }

        public Block Get(IGuild guild, BlockType blockType)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return Get(guild.Id, blockType);
        }

        public Block Get(ulong guildId, BlockType blockType)
            => Context.Blocks.FirstOrDefault(block => block.GuildId == guildId && block.BlockType == blockType);

        public async Task<Block> GetAsync(IGuild guild, BlockType blockType)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetAsync(guild.Id, blockType);
        }

        public async Task<Block> GetAsync(ulong guildId, BlockType blockType)
            => await Context.Blocks.FirstOrDefaultAsync(block => block.GuildId == guildId && block.BlockType == blockType);

        public IEnumerable<Block> GetAll(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return GetAll(guild.Id);
        }

        public IEnumerable<Block> GetAll(ulong guildId)
            => Context.Blocks.Where(block => block.GuildId == guildId).ToList();

        public async Task<IEnumerable<Block>> GetAllAsync(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await GetAllAsync(guild.Id);
        }

        public async Task<IEnumerable<Block>> GetAllAsync(ulong guildId)
            => await Context.Blocks.Where(block => block.GuildId == guildId).ToListAsync();
    }
}