using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace NoAdsHere.Database.Models.Global
{
    public class Master : DatabaseBase, IIndexed
    {
        public Master(ulong userId)
        {
            UserId = userId;
        }

        public ObjectId Id { get; set; }
        public ulong UserId { get; set; }

        internal async Task<bool> DeleteAsync()
        {
            var collection = Mongo.GetCollection<Master>(Client);
            
            await collection.DeleteAsync(this);
            return true;
        }

        internal async Task<bool> InsertAsync()
        {
            var collection = Mongo.GetCollection<Master>(Client);
            var masters = await collection.GetMasterAsync(UserId);

            if (masters != null) return false;
            await collection.InsertOneAsync(this);
            return true;
        }
    }
}