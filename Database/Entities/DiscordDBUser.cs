using System.Collections.Generic;

namespace Database.Entities
{
    public class DiscordDbUser
    {
        public ulong UserId { get; set; }
        public int Violations { get; set; }
        public List<string> RemovedInvites { get; set; }
    }
}