using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Services.Database;
using System.Threading.Tasks;

namespace NoAdsHere.Database.Models.GuildSettings
{
    [BsonIgnoreExtraElements]
    public class Block : DatabaseService, IIndexed
    {
        public Block(ulong guildId, BlockType type)
        {
            GuildId = guildId;
            BlockType = type;
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public BlockType BlockType { get; set; }

        internal async Task<DeleteResult> DeleteAsync()
        {
            var collection = _db.GetCollection<Block>();
            return await collection.DeleteOneAsync(i => i.Id == Id);
        }

        internal async Task<ReplaceOneResult> UpdateAsync()
        {
            var collection = _db.GetCollection<Block>();
            return await collection.ReplaceOneAsync(i => i.Id == Id, this, new UpdateOptions { IsUpsert = true });
        }
    }
}