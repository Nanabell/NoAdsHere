using System.Collections.Generic;

namespace Database.Entities.InviteWhitelist
{
    public class ChannelWhitelist
    {
        /// <summary>
        /// Guild this Whitelist entry belongs to
        /// </summary>
        public ulong GuildId { get; set; }
        
        /// <summary>
        /// Target ChannelId this whitelist is valid for
        /// </summary>
        public ulong ChannelId { get; set; }
        
        /// <summary>
        /// Optional List of InviteIds that should be Whitelisted, leave empty to whitelist all
        /// </summary>
        public List<string> InviteIds { get; set; } = new List<string>();
    }
}