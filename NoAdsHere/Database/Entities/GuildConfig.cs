using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace NoAdsHere.Database.Entities
{
    public class GuildConfig
    {
        public GuildConfig(ulong id)
        {
            Id = id;
        }

        public ulong Id { get; set; }
        
        [JsonProperty(Required = Required.AllowNull)]
        public List<AntiAd> AntiAds { get; set; } = new List<AntiAd>();
        
        [JsonProperty(Required = Required.AllowNull)]
        public List<ulong> GlobalWhitelist { get; set; } = new List<ulong>();
        
        [JsonProperty(Required = Required.AllowNull)]
        public List<Faq> Faqs { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string GithubRepository { get; set; }
    }
}