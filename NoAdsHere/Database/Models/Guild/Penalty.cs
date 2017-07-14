using MongoDB.Bson;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Services.Database;
using System.Threading.Tasks;

namespace NoAdsHere.Database.Models.GuildSettings
{
    public class Penalty : DatabaseService, IIndexed
    {
        public Penalty(ulong guildId, int penaltyId, PenaltyType penaltyType, int requiredPoints,
            string message = null, bool autoDelete = false)
        {
            GuildId = guildId;
            PenaltyId = penaltyId;
            PenaltyType = penaltyType;
            RequiredPoints = requiredPoints;
            Message = message;
            AutoDelete = autoDelete;
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public int PenaltyId { get; set; }
        public PenaltyType PenaltyType { get; set; }
        public int RequiredPoints { get; set; }
        public string Message { get; set; }
        public bool AutoDelete { get; set; }

        internal async Task<DeleteResult> DeleteAsync()
        {
            var collection = _db.GetCollection<Penalty>();
            return await collection.DeleteOneAsync(i => i.Id == Id);
        }

        internal async Task<ReplaceOneResult> UpdateAsync()
        {
            var collection = _db.GetCollection<Penalty>();
            return await collection.ReplaceOneAsync(i => i.Id == Id, this, new UpdateOptions { IsUpsert = true });
        }
    }
}