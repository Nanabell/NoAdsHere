using MongoDB.Bson;
using NoAdsHere.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace NoAdsHere.Services.Penalties
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