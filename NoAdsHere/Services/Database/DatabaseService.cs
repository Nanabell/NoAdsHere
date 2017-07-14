using Discord;
using MongoDB.Driver;
using NoAdsHere.Common;
using NoAdsHere.Database;
using NoAdsHere.Database.Models.Global;
using NoAdsHere.Database.Models.Guild;
using NoAdsHere.Database.Models.GuildSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Services.Database
{
    public class DatabaseService
    {
        protected static IMongoDatabase _db { get; set; }

        public DatabaseService()
        {
        }

        public DatabaseService(IMongoClient mongo, string dbName)
        {
            _db = mongo.GetDatabase(dbName);
        }

        internal void LoadDatabase(IMongoClient mongo, string dbName)
        {
            _db = mongo.GetDatabase(dbName);
        }

        internal IMongoCollection<T> GetCollection<T>()
            => _db.GetCollection<T>();

        internal async Task InsertOneAsync<T>(T entity)
        {
            await _db.GetCollection<T>().InsertOneAsync(entity);
        }

        internal async Task InsertManyAsync<T>(IEnumerable<T> entities)
        {
            await _db.GetCollection<T>().InsertManyAsync(entities);
        }

        internal async Task<Master> GetMasterAsync(ulong userId)
        {
            var collection = _db.GetCollection<Master>();
            var cursor = await collection.FindAsync(filter => filter.UserId == userId);
            return await cursor.SingleOrDefaultAsync();
        }

        internal async Task<List<Master>> GetMastersAsync()
        {
            var collection = _db.GetCollection<Master>();
            var cursor = await collection.FindAsync(filter => true);
            return await cursor.ToListAsync();
        }

        internal async Task<Statistics> GetStatisticsAsync(ulong guildId)
        {
            var collection = _db.GetCollection<Statistics>();
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
            var collection = _db.GetCollection<Statistics>();
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

        internal async Task<Block> GetBlockAsync(ulong guildId, BlockType blockType)
        {
            var collection = _db.GetCollection<Block>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.BlockType == blockType);
            var result = await cursor.SingleOrDefaultAsync();

            if (result != null)
                return result;

            await collection.InsertOneAsync(new Block(guildId, blockType));
            cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.BlockType == blockType);
            return await cursor.SingleAsync();
        }

        internal async Task<List<Block>> GetBlocksAsync(ulong guildId)
        {
            var collection = _db.GetCollection<Block>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        internal async Task<List<Ignore>> GetIgnoresAsync(ulong guildId, BlockType blockType)
        {
            var collection = _db.GetCollection<Ignore>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && (filter.BlockType == blockType || filter.BlockType == BlockType.All));
            return await cursor.ToListAsync();
        }

        internal async Task<List<Ignore>> GetUserIgnoresAsync(ulong guildId, BlockType blockType)
        {
            var collection = _db.GetCollection<Ignore>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.IgnoreType == IgnoreType.User && (filter.BlockType == blockType || filter.BlockType == BlockType.All));
            return await cursor.ToListAsync();
        }

        internal async Task<List<Ignore>> GetRoleIgnoresAsync(ulong guildId, BlockType blockType)
        {
            var collection = _db.GetCollection<Ignore>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.IgnoreType == IgnoreType.Role && (filter.BlockType == blockType || filter.BlockType == BlockType.All));
            return await cursor.ToListAsync();
        }

        internal async Task<List<Ignore>> GetChannelIgnoresAsync(ulong guildId, BlockType blockType)
        {
            var collection = _db.GetCollection<Ignore>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.IgnoreType == IgnoreType.Channel && (filter.BlockType == blockType || filter.BlockType == BlockType.All));
            return await cursor.ToListAsync();
        }

        internal async Task<Penalty> GetPenaltyAsync(ulong guildId, int penaltyId)
        {
            var collection = _db.GetCollection<Penalty>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId && filter.PenaltyId == penaltyId);
            return await cursor.SingleOrDefaultAsync();
        }

        internal async Task<List<Penalty>> GetPenaltiesAsync(ulong guildId)
        {
            var collection = _db.GetCollection<Penalty>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        internal async Task<Violator> GetViolatorAsync(ulong guildId, ulong userId)
        {
            var collection = _db.GetCollection<Violator>();
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
            var collection = _db.GetCollection<Violator>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.ToListAsync();
        }

        internal async Task<List<AllowString>> GetIgnoreStringsAsync(ulong guildId)
        {
            var collection = _db.GetCollection<AllowString>();
            var cursor = await collection.FindAsync(filter => filter.GuildId == guildId);
            return await cursor.ToListAsync();
        }
    }
}