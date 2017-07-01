using System;
using System.Threading.Tasks;
using Discord;
using MongoDB.Bson;
using MongoDB.Driver;
using NoAdsHere.Database.Models.GuildSettings;
using NoAdsHere.Database.Models.Violator;
using System.Collections.Generic;
using Discord.WebSocket;
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

        public static async Task<ReplaceOneResult> SaveAsync<T>(this IMongoCollection<T> collection, T entity) where T : IIndexed
        {
            return await collection.ReplaceOneAsync(i => i.Id == entity.Id, entity, new UpdateOptions { IsUpsert = true });
        }

        public static async Task<DeleteResult> DeleteAsync<T>(this IMongoCollection<T> collection, T entity) where T : IIndexed
        {
            return await collection.DeleteOneAsync(i => i.Id == entity.Id);
        }

        public static async Task<T> SingleByGuildAsync<T>(this IMongoCollection<T> collection, ulong guildId) where T : IGuildIndexed
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId);
            var result = await cursor.SingleOrDefaultAsync();

            if (result != null)
                return result;
            return default(T);
        }

        public static async Task<List<T>> ListByGuildAsync<T>(this IMongoCollection<T> collection, ulong guildId) where T : IGuildIndexed
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId);
            var result = await cursor.ToListAsync();

            return result;
        }

        public static async Task<Block> GetBlockAsync(this IMongoCollection<Block> collection, ulong guildId, BlockType type)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId && f.BlockType == type);
            var result = await cursor.SingleOrDefaultAsync();

            if (result != null)
                return result;
            await collection.InsertOneAsync(new Block(guildId, type));
            var cursor2 = await collection.FindAsync(f => f.GuildId == guildId && f.BlockType == type);
            var result2 = await cursor2.SingleOrDefaultAsync();
            return result2;
        }

        public static async Task<List<Block>> GetGuildBlocksAsync(this IMongoCollection<Block> collection,
            ulong guildId)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        public static async Task<List<Block>> GetBlocksAsync(this IMongoCollection<Block> collection)
        {
            var cursor = await collection.FindAsync("{}");
            return await cursor.ToListAsync();
        }

        public static async Task<List<Ignore>> GetIgnoresAsync(this IMongoCollection<Ignore> collection, ulong guildId, BlockType type)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId && (f.BlockType == type || f.BlockType == BlockType.All));
            var result = await cursor.ToListAsync();

            return result ?? new List<Ignore>(0);
        }

        public static async Task<Penalty> GetPenaltyAsync(this IMongoCollection<Penalty> collection, ulong guildId, int penaltyId)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId && f.PenaltyId == penaltyId);
            var result = await cursor.SingleOrDefaultAsync();
            return result;
        }

        public static async Task<List<Penalty>> GetPenaltiesAsync(this IMongoCollection<Penalty> collection, ulong guildId)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId);
            var result = await cursor.ToListAsync();
            return result;
        }

        public static async Task<Violator> GetUserAsync(this IMongoCollection<Violator> collection, ulong guildId, ulong userid)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId && f.UserId == userid);
            var result = await cursor.SingleOrDefaultAsync();

            if (result != null)
                return result;
            await collection.InsertOneAsync(new Violator(guildId, userid));
            var cursor2 = await collection.FindAsync(f => f.GuildId == guildId && f.UserId == userid);
            var result2 = await cursor2.SingleOrDefaultAsync();
            return result2;
        }

        public static async Task<List<Violator>> GetAllByGuildAsync(this IMongoCollection<Violator> collection,
            ulong guildId)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        public static async Task<List<Master>> GetMastersAsync(this IMongoCollection<Master> collection)
        {
            var cursor = await collection.FindAsync("{}");
            return await cursor.ToListAsync();
        }

        public static async Task<Master> GetMasterAsync(this IMongoCollection<Master> collection, ulong userId)
        {
            var cursor = await collection.FindAsync(f => f.UserId == userId);
            return await cursor.SingleOrDefaultAsync();
        }

        public static async Task<Stats> GetGuildStatsAsync(this IMongoCollection<Stats> collection, IGuild guild)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guild.Id);
            var result = await cursor.FirstOrDefaultAsync();

            if (result != null)
                return result;
            await collection.InsertOneAsync(new Stats(guild.Id));
            var cursor2 = await collection.FindAsync(f => f.GuildId == guild.Id);
            return await cursor2.FirstOrDefaultAsync();
        }

        public static async Task<List<Stats>> GetAllStatsAsync(this IMongoCollection<Stats> collection)
        {
            var cursor = await collection.FindAsync("{}");
            return await cursor.ToListAsync();
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