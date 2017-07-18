using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Database.Models.FAQ
{
    public class GuildFaqEntry : DatabaseService, IIndexed
    {
        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong CreatorId { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public uint UseCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }

        internal async Task<DeleteResult> DeleteAsync()
        {
            var collection = Db.GetCollection<GuildFaqEntry>();
            return await collection.DeleteOneAsync(i => i.Id == Id);
        }

        internal async Task<ReplaceOneResult> UpdateAsync()
        {
            var collection = Db.GetCollection<GuildFaqEntry>();
            return await collection.ReplaceOneAsync(i => i.Id == Id, this, new UpdateOptions { IsUpsert = true });
        }
    }
}