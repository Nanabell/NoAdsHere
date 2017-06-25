using System.Threading.Tasks;
using MongoDB.Bson;

namespace NoAdsHere.Database.Models.GuildSettings
{
    public class Stats : DatabaseBase, IIndexed
    {
        public Stats(ulong guildId)
        {
            GuildId = guildId;
        }
        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public uint Blocks { get; set; }
        public uint Warns { get; set; }
        public uint Kicks { get; set; }
        public uint Bans { get; set; }

        internal async Task SaveAsync()
        {
            var collection = Mongo.GetCollection<Stats>(Client);
            await collection.SaveAsync(this);
        }
    }
}