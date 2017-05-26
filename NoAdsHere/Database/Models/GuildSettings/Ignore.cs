using MongoDB.Bson;

namespace NoAdsHere.Database.Models.GuildSettings
{
    public enum IgnoreTypes
    {
        Disabled,
        User,
        Role,
        Channel,
        All
    }

    public enum IgnoreingTypes
    {
        Disabled = 0,
        Invites = 10,
        Youtube,
        Twitch,
        All = 256
    }

    public class Ignore : IGuildIndexed
    {
        public Ignore(ulong guildId, IgnoreTypes ignoreType, ulong ignoredId, IgnoreingTypes ignoringType)
        {
            GuildId = guildId;
            IgnoreType = ignoreType;
            IgnoredId = ignoredId;
            IgnoreingType = ignoringType;
        }
        
        
        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public IgnoreTypes IgnoreType { get; set; }
        public ulong IgnoredId { get; set; }
        public IgnoreingTypes IgnoreingType { get; set; }
    }
}