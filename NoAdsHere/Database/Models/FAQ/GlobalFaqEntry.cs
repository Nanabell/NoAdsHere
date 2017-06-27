using System;
using MongoDB.Bson;

namespace NoAdsHere.Database.Models.FAQ
{
    public class GlobalFaqEntry : IIndexed
    {
        public ObjectId Id { get; set; }
        public ulong CreatorId { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public uint UseCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }
    }
}