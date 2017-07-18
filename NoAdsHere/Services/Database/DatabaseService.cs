using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.Global;
using NoAdsHere.Database.Models.Guild;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NoAdsHere.Database.Models.FAQ;

namespace NoAdsHere.Services.Database
{
    public class DatabaseService
    {
        protected static IMongoDatabase Db { get; private set; }

        protected DatabaseService()
        {
        }

        public DatabaseService(IMongoClient mongo, string dbName)
        {
            Db = mongo.GetDatabase(dbName);
        }

        internal void LoadDatabase(IMongoClient mongo, string dbName)
        {
            Db = mongo.GetDatabase(dbName);
        }

        internal IMongoCollection<T> GetCollection<T>()
            => Db.GetCollection<T>();

        internal async Task InsertOneAsync<T>(T entity)
        {
            await Db.GetCollection<T>().InsertOneAsync(entity);
        }

        internal async Task InsertManyAsync<T>(IEnumerable<T> entities)
        {
            await Db.GetCollection<T>().InsertManyAsync(entities);
        }

        internal async Task<Master> GetMasterAsync(ulong userId)
        {
            var collection = Db.GetCollection<Master>();
            var cursor = await collection.FindAsync(filter => filter.UserId == userId);
            return await cursor.SingleOrDefaultAsync();
        }

        internal async Task<List<Master>> GetMastersAsync()
        {
            var collection = Db.GetCollection<Master>();
            var cursor = await collection.FindAsync(filter => true);
            return await cursor.ToListAsync();
        }

        internal async Task<Statistics> GetStatisticsAsync(ulong guildId)
        {
            var collection = Db.GetCollection<Statistics>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            var result = await cursor.SingleOrDefaultAsync();

            if (result != null)
                return result;

            await collection.InsertOneAsync(new Statistics(guildId));
            cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.SingleAsync();
        }

        internal async Task<Statistics> GetStatisticsAsync()
        {
            var collection = Db.GetCollection<Statistics>();
            var cursor = await collection.FindAsync(filter => true);
            var result = await cursor.ToListAsync();

            return new Statistics
            {
                Blocks = (uint)result.Sum(s => s.Blocks),
                Warns = (uint)result.Sum(s => s.Warns),
                Kicks = (uint)result.Sum(s => s.Kicks),
                Bans = (uint)result.Sum(s => s.Bans)
            };
        }

        internal async Task<Block> GetBlockAsync(ulong guildId, BlockType blockType, bool createNew = true)
        {
            var collection = Db.GetCollection<Block>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.BlockType == blockType);
            var result = await cursor.SingleOrDefaultAsync();

            if (result != null || !createNew)
                return result;

            await collection.InsertOneAsync(new Block(guildId, blockType));
            cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.BlockType == blockType);
            return await cursor.SingleAsync();
        }

        internal async Task<List<Block>> GetBlocksAsync(ulong guildId)
        {
            var collection = Db.GetCollection<Block>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        internal async Task<List<Ignore>> GetUserIgnoresAsync(ulong guildId)
        {
            var collection = Db.GetCollection<Ignore>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.IgnoreType == IgnoreType.User);
            return await cursor.ToListAsync();
        }

        internal async Task<List<Ignore>> GetRoleIgnoresAsync(ulong guildId)
        {
            var collection = Db.GetCollection<Ignore>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.IgnoreType == IgnoreType.Role);
            return await cursor.ToListAsync();
        }

        internal async Task<List<Ignore>> GetChannelIgnoresAsync(ulong guildId)
        {
            var collection = Db.GetCollection<Ignore>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.IgnoreType == IgnoreType.Channel);
            return await cursor.ToListAsync();
        }

        internal async Task<Penalty> GetPenaltyAsync(ulong guildId, int penaltyId)
        {
            var collection = Db.GetCollection<Penalty>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.PenaltyId == penaltyId);
            return await cursor.SingleOrDefaultAsync();
        }

        internal async Task<List<Penalty>> GetPenaltiesAsync(ulong guildId)
        {
            var collection = Db.GetCollection<Penalty>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        internal async Task<Violator> GetViolatorAsync(ulong guildId, ulong userId)
        {
            var collection = Db.GetCollection<Violator>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.UserId == userId);
            var result = await cursor.SingleOrDefaultAsync();

            if (result != null)
                return result;

            await collection.InsertOneAsync(new Violator(guildId, userId));
            cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.UserId == userId);
            return await cursor.SingleAsync();
        }

        internal async Task<List<Violator>> GetViolatorsAsync(ulong guildId)
        {
            var collection = Db.GetCollection<Violator>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        internal async Task<List<AllowString>> GetIgnoreStringsAsync(ulong guildId)
        {
            var collection = Db.GetCollection<AllowString>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        public async Task<GuildFaqEntry> GetGuildFaqAsync(ulong guildId, string faq)
        {
            var collection = Db.GetCollection<GuildFaqEntry>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.Name == faq.ToLower());
            return await cursor.SingleOrDefaultAsync();
        }

        public async Task<List<GuildFaqEntry>> GetGuildFaqsAsync(ulong guildId)
        {
            var collection = Db.GetCollection<GuildFaqEntry>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        public async Task<GlobalFaqEntry> GetGlobalFaqAsync(string faq)
        {
            var collection = Db.GetCollection<GlobalFaqEntry>();
            var cursor = await collection.FindAsync(filter => filter.Name == faq.ToLower());
            return await cursor.SingleOrDefaultAsync();
        }

        public async Task<List<GlobalFaqEntry>> GetGlobalFaqsAsync()
        {
            var collection = Db.GetCollection<GlobalFaqEntry>();
            var cursor = await collection.FindAsync("{}");
            return await cursor.ToListAsync();
        }
    }
}