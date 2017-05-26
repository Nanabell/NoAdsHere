using System;
using MongoDB.Bson;

namespace NoAdsHere.Database.Models.Violator
{
    public class Violator : IIndexed
    {
        public Violator(ulong guildId, ulong userId)
        {
            GuildId = guildId;
            UserId = userId;
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public int Points { get; set; }
        public DateTime LatestViolation { get; set; } = DateTime.Now;
    }
}