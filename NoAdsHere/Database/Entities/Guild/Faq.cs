using System;

namespace NoAdsHere.Database.Entities.Guild
{
    public class Faq
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong CreatorId { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public uint UseCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }
    }
}