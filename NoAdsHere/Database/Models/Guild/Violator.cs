using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Database.Models.Guild
{
    public class Violator : DatabaseService, IIndexed
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
        public DateTime LatestViolation { get; set; } = DateTime.UtcNow;

        internal async Task<DeleteResult> DeleteAsync()
        {
            var collection = _db.GetCollection<Violator>();
            return await collection.DeleteOneAsync(i => i.Id == Id);
        }

        internal async Task<ReplaceOneResult> UpdateAsync()
        {
            var collection = _db.GetCollection<Violator>();
            return await collection.ReplaceOneAsync(i => i.Id == Id, this, new UpdateOptions { IsUpsert = true });
        }
    }
}