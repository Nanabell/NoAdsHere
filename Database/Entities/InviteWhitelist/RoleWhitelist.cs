using System.Collections.Generic;

namespace Database.Entities.InviteWhitelist
{
    public class RoleWhitelist
    {
        /// <summary>
        /// Part of the primary key to define whom this whitelist belongs to
        /// </summary>
        public ulong GuildId { get; set; }
        
        /// <summary>
        /// Part of the primary key to define whom this whitelist belongs to
        /// </summary>
        public ulong RoleId { get; set; }
        
        /// <summary>
        /// Optional Target ChannelId this whitelist is valid for
        /// </summary>
        public ulong ChannelId { get; set; }
        
        /// <summary>
        /// Optional List of InviteIds that should be Whitelisted, leave empty to whitelist all
        /// </summary>
        public List<string> InviteIds { get; set; } = new List<string>();
    }
}