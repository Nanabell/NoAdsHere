using System.Collections.Generic;

namespace NoAdsHere.Database.Entities
{
    public class Config
    {
        public string Token { get; set; }
        public PrefixConfir Prefix { get; set; }
        public int Shards { get; set; }
        public WebhookConfig Webhook { get; set; }
        public List<ulong> Masters { get; set; }
        
        public class PrefixConfir
        {
            public string Main { get; set; }
            public string Faq { get; set; }
        }

        public class WebhookConfig
        {
            public ulong Id { get; set; }
            public string Token { get; set; }
        }
    }
}