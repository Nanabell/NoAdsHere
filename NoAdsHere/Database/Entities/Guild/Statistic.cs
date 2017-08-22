using System.ComponentModel.DataAnnotations;
using Discord;

namespace NoAdsHere.Database.Entities.Guild
{
    public class Statistic
    {
        public Statistic()
        {
        }

        public Statistic(IGuild guild)
        {
            GuildId = guild.Id;
        }

        public Statistic(ulong guildId)
        {
            GuildId = guildId;
        }

        public int Id { get; set; }
        public ulong GuildId { get; set; }

        public uint Blocks { get; set; }
        public uint Warns { get; set; }
        public uint Kicks { get; set; }
        public uint Bans { get; set; }
    }
}