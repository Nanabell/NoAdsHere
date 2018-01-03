using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace NoAdsHere.Database.Entities
{
    public class AntiAd
    {
        public AntiAd(ulong channelId, Regex adRegex)
        {
            ChannelId = channelId;
            AdRegex = adRegex;
        }
        
        public ulong ChannelId { get; set; }
        
        [JsonProperty(Required = Required.Always)]
        public Regex AdRegex { get; set; }

        public uint Priority { get; set; }
        
        [JsonProperty(Required = Required.AllowNull)]
        public List<ulong> Whitelist { get; set; } = new List<ulong>();
    }
}