using MongoDB.Bson;
using NoAdsHere.Common;

namespace NoAdsHere.Database.Models.GuildSettings
{
    public class Penalty : IIndexed
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
    }
}