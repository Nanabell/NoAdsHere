using System;
using Discord;

namespace NoAdsHere.Database.Entities.Guild
{
    public class Violator
    {
        public Violator(IGuildUser user)
        {
            GuildId = user.GuildId;
            UserId = user.Id;
        }

        public Violator()
        {
        }

        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public int Points { get; set; }
        public DateTime LatestViolation { get; set; } = DateTime.UtcNow;
    }
}