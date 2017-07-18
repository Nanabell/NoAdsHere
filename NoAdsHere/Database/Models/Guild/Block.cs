using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Database.Models.Guild
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
            var collection = Db.GetCollection<Block>();
            return await collection.DeleteOneAsync(i => i.Id == Id);
        }

        internal async Task<ReplaceOneResult> UpdateAsync()
        {
            var collection = Db.GetCollection<Block>();
            return await collection.ReplaceOneAsync(i => i.Id == Id, this, new UpdateOptions { IsUpsert = true });
        }
    }
}