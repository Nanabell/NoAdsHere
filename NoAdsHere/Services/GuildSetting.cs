using MongoDB.Bson;
using NoAdsHere.Common;
using System.Collections.Generic;

namespace NoAdsHere.Services
{
    public class GuildSetting : IIndexed
    {
        public GuildSetting(ulong guildId)
        {
            GuildId = guildId;
        }

        public ObjectId Id { get; set; }
        public ulong GuildId { get; set; }

        public Ignores Ignorings { get; set; } = new Ignores();

        public class Ignores
        {
            public List<ulong> Users { get; set; } = new List<ulong>(0);
            public List<ulong> Roles { get; set; } = new List<ulong>(0);
            public List<ulong> Channels { get; set; } = new List<ulong>(0);
        }

        public Blocks Blockings { get; set; } = new Blocks();

        public class Blocks
        {
            public bool Invites { get; set; } = true;
            public bool Youtube { get; set; } = false;
            public bool Twitch { get; set; } = false;
        }

        public Penalties Penaltings { get; set; } = new Penalties();

        public class Penalties
        {
            public int InfoMessage { get; set; } = 1;
            public int WarnMessage { get; set; } = 3;
            public int Kick { get; set; } = 5;
            public int Ban { get; set; } = 6;
        }
    }
}