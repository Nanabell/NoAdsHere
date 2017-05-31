using MongoDB.Bson;

namespace NoAdsHere.Database.Models.Settings
{
    public class Master
    {
        public ObjectId Id { get; set; }
        public ulong UserId { get; set; }
    }
}