using System;
using System.Threading.Tasks;
using Discord;
using MongoDB.Bson;
using MongoDB.Driver;
using NoAdsHere.Database.Models.GuildSettings;
using System.Collections.Generic;
using NoAdsHere.Common;
using NoAdsHere.Database.Models.FAQ;
using NoAdsHere.Database.Models.Global;

namespace NoAdsHere.Database
{
    public interface IIndexed
    {
        ObjectId Id { get; set; }
    }

    public interface IGuildIndexed : IIndexed
    {
        ulong GuildId { get; set; }
    }

    public static class DatabaseExtensions
    {
        public static IMongoCollection<T> GetCollection<T>(this MongoClient mongo, IDiscordClient client)
        {
            if (client.CurrentUser == null) throw new ArgumentNullException(nameof(client.CurrentUser));
            var dbname = client.CurrentUser.Username.Replace(" ", "");
            var db = mongo.GetDatabase(dbname);
            return db.GetCollection<T>(typeof(T).Name);
        }

        public static IMongoCollection<T> GetCollection<T>(this IMongoDatabase db)
            => db.GetCollection<T>(typeof(T).Name);

        public static async Task<ReplaceOneResult> SaveAsync<T>(this IMongoCollection<T> collection, T entity) where T : IIndexed
        {
            return await collection.ReplaceOneAsync(i => i.Id == entity.Id, entity, new UpdateOptions { IsUpsert = true });
        }

        public static async Task<DeleteResult> DeleteAsync<T>(this IMongoCollection<T> collection, T entity) where T : IIndexed
        {
            return await collection.DeleteOneAsync(i => i.Id == entity.Id);
        }

        public static async Task<GuildFaqEntry> GetGuildFaqAsync(this IMongoCollection<GuildFaqEntry> collection,
            ulong guildId, string name)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId && f.Name == name.ToLower());
            return await cursor.SingleOrDefaultAsync();
        }

        public static async Task<List<GuildFaqEntry>> GetGuildFaqsAsync(this IMongoCollection<GuildFaqEntry> collection,
            ulong guildId)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        public static async Task<List<GlobalFaqEntry>> GetGlobalFaqsAsync(this IMongoCollection<GlobalFaqEntry> collection)
        {
            var cursor = await collection.FindAsync("{}");
            return await cursor.ToListAsync();
        }

        public static async Task<GlobalFaqEntry> GetGlobalFaqAsync(this IMongoCollection<GlobalFaqEntry> collection, string name)
        {
            var cursor = await collection.FindAsync(f => f.Name == name.ToLower());
            return await cursor.SingleOrDefaultAsync();
        }
    }
}