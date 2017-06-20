using MongoDB.Bson;

namespace NoAdsHere.Database.Models.Global
{
    public class Master : IIndexed
    {
        public Master(ulong userId)
        {
            UserId = userId;
        }

        public ObjectId Id { get; set; }
        public ulong UserId { get; set; }
    }
}