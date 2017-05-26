using MongoDB.Bson;

namespace NoAdsHere.Database.Models.GuildSettings
{
    public enum PenaltyTypes
    {
        Nothing,
        InfoMessage,
        WarnMessage,
        Kick,
        Ban
    }

    public class Penalty : IIndexed
    {
        public Penalty(ulong guildId, int penaltyId, PenaltyTypes penaltyType, int requiredPoints,
            string message = null)
        {
            GuildId = guildId;
            PenaltyId = penaltyId;
            PenaltyType = penaltyType;
            RequiredPoints = requiredPoints;
            Message = Message;
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public int PenaltyId { get; set; }
        public PenaltyTypes PenaltyType { get; set; }
        public int RequiredPoints { get; set; }
        public string Message { get; set; }
    }
}