using MongoDB.Bson;
using NoAdsHere.Common;

namespace NoAdsHere.Database.Models.GuildSettings
{
    public class Ignore : IGuildIndexed
    {
        public Ignore(ulong guildId, IgnoreType ignoreType, ulong ignoredId, BlockType blockType)
        {
            GuildId = guildId;
            IgnoreType = ignoreType;
            IgnoredId = ignoredId;
            BlockType = blockType;
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public IgnoreType IgnoreType { get; set; }
        public ulong IgnoredId { get; set; }
        public BlockType BlockType { get; set; }
    }
}