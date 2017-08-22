using Discord;
using NoAdsHere.Common;
using System.ComponentModel.DataAnnotations;

namespace NoAdsHere.Database.Entities.Guild
{
    public class Penalty
    {
        public Penalty(IGuild guild, PenaltyType penaltyType, int requiredPoints, string message = null, bool autoDelete = false)
        {
            GuildId = guild.Id;
            PenaltyType = penaltyType;
            RequiredPoints = requiredPoints;
            Message = message;
            AutoDelete = autoDelete;
        }

        public Penalty(ulong guildId, PenaltyType penaltyType, int requiredPoints, string message = null, bool autoDelete = false)
        {
            GuildId = guildId;
            PenaltyType = penaltyType;
            RequiredPoints = requiredPoints;
            Message = message;
            AutoDelete = autoDelete;
        }

        public Penalty()
        {
        }

        [Key]
        public int Id { get; set; }

        public ulong GuildId { get; set; }
        public PenaltyType PenaltyType { get; set; }
        public int RequiredPoints { get; set; }
        public string Message { get; set; }
        public bool AutoDelete { get; set; }
    }
}