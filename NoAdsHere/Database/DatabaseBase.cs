using Discord.WebSocket;
using MongoDB.Driver;

namespace NoAdsHere.Database
{
    public abstract class DatabaseBase
    {
        public static MongoClient Mongo;
        public static DiscordSocketClient Client;
    }
}