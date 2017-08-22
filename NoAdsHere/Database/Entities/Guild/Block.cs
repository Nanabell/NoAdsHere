using Discord;
using NoAdsHere.Common;

namespace NoAdsHere.Database.Entities.Guild
{
    public class Block
    {
        public Block(IGuild guild, BlockType type)
        {
            GuildId = guild.Id;
            BlockType = type;
        }

        public Block(ulong guildId, BlockType type)
        {
            GuildId = guildId;
            BlockType = type;
        }

        public Block()
        {
        }

        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public BlockType BlockType { get; set; }
    }
}