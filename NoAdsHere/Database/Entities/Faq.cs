using System;

namespace NoAdsHere.Database.Entities
{
    public class Faq
    {
        public Faq(ulong guildId, ulong creatorId, string name, string content)
        {
            GuildId = guildId;
            CreatorId = creatorId;
            Name = name;
            Content = content;
        }

        public ulong GuildId { get; set; }
        public ulong CreatorId { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public uint Uses { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastUsed { get; set; } = DateTimeOffset.MinValue;
    }
}