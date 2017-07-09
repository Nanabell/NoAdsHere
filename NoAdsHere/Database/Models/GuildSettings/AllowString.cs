using System;
using MongoDB.Bson;
using NoAdsHere.Common;

namespace NoAdsHere.Database.Models.GuildSettings
{
    public class AllowString: IIndexed
    {
        public AllowString(ulong guildId, IgnoreType ignoreType, ulong ignoredId, string allowedString)
        {
            GuildId = guildId;
            IgnoreType = ignoreType;
            IgnoredId = ignoredId;
            AllowedString = allowedString ?? throw new ArgumentNullException(nameof(allowedString));
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public IgnoreType IgnoreType { get; set; }
        public ulong IgnoredId { get; set; }
        public string AllowedString { get; set; }
    }
}