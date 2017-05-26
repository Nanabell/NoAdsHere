using MongoDB.Bson;

namespace NoAdsHere.Database.Models.GuildSettings
{
    public enum BlockTypes
    {
        Invites,
        Youtube,
        Twitch
    }

    public class Block : IIndexed
    {
        public Block(ulong guildId, BlockTypes type, bool enabled = false)
        {
            GuildId = guildId;
            BlockType = type;
            IsEnabled = enabled;
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public BlockTypes BlockType { get; set; }
        public bool IsEnabled { get; set; }
    }
}