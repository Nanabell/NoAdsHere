using Discord.WebSocket;
using MongoDB.Driver;

namespace NoAdsHere.Database
{
    public abstract class DatabaseBase
    {
        public static MongoClient Mongo { get; set; }

        public static DiscordShardedClient Client { get; set; }
    }
}