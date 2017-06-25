using System;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace NoAdsHere.Database.Models.Violator
{
    public class Violator : DatabaseBase, IIndexed
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

        internal async Task<bool> InsertAsync()
        {
            var collection = Mongo.GetCollection<Violator>(Client);
            var violator = await collection.GetUserAsync(GuildId, UserId);

            if (violator != null) return false;
            await collection.InsertOneAsync(this);
            return true;
        }

        internal async Task SaveAsync()
        {
            var collection = Mongo.GetCollection<Violator>(Client);
            await collection.SaveAsync(this);
        }

        internal async Task DeleteAsync()
        {
            var collection = Mongo.GetCollection<Violator>(Client);
            await collection.DeleteAsync(this);
        }
    }
}