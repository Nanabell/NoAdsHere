using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NoAdsHere.Services.Database;
using System;

namespace NoAdsHere.Database.Models.Global
{
    public class Master : DatabaseService, IIndexed
    {
        public Master(ulong userId)
        {
            UserId = userId;
        }

        public ObjectId Id { get; set; }
        public ulong UserId { get; set; }

        internal async Task InsertAsync()
        {
            var collection = _db.GetCollection<Master>();
            var master = GetMasterAsync(UserId);

            if (master == null)
            {
                await collection.InsertOneAsync(this);
            }
            else
            {
                throw new ArgumentException(nameof(master));
            }
        }

        internal async Task<DeleteResult> DeleteAsync()
        {
            var collection = _db.GetCollection<Master>();
            return await collection.DeleteOneAsync(i => i.Id == Id);
        }

        internal async Task<ReplaceOneResult> UpdateAsync()
        {
            var collection = _db.GetCollection<Master>();
            return await collection.ReplaceOneAsync(i => i.Id == Id, this, new UpdateOptions { IsUpsert = true });
        }
    }
}