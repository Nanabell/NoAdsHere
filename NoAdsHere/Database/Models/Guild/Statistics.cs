using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Database.Models.Guild
{
    public class Statistics : DatabaseService, IIndexed
    {
        public Statistics()
        {
        }

        public Statistics(ulong guildId)
        {
            GuildId = guildId;
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public uint Blocks { get; set; }
        public uint Warns { get; set; }
        public uint Kicks { get; set; }
        public uint Bans { get; set; }

        internal async Task<DeleteResult> DeleteAsync()
        {
            var collection = Db.GetCollection<Statistics>();
            return await collection.DeleteOneAsync(i => i.Id == Id);
        }

        internal async Task<ReplaceOneResult> UpdateAsync()
        {
            var collection = Db.GetCollection<Statistics>();
            return await collection.ReplaceOneAsync(i => i.Id == Id, this, new UpdateOptions { IsUpsert = true });
        }
    }
}