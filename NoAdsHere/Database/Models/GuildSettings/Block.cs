using MongoDB.Bson;
using NoAdsHere.Common;

namespace NoAdsHere.Database.Models.GuildSettings
{
    public class Block : IIndexed
    {
        public Block(ulong guildId, BlockType type, bool enabled = false)
        {
            GuildId = guildId;
            BlockType = type;
            IsEnabled = enabled;
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public BlockType BlockType { get; set; }
        public bool IsEnabled { get; set; }
    }
}