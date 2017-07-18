using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MoreLinq;
using NoAdsHere.Common;
using NoAdsHere.Database.Models.FAQ;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Services.FAQ
{
    public class FaqSystem
    {
        private readonly DatabaseService _database;

        public FaqSystem(DatabaseService database)
        {
            _database = database;
        }

        public async Task<bool> AddGuildEntryAsync(ulong guildId, ulong userId, string name, string response)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(response)) throw new ArgumentNullException(nameof(response));
            var lEntry = new GuildFaqEntry
            {
                GuildId = guildId,
                CreatorId = userId,
                Name = name.ToLower(),
                Content = response,
                CreatedAt = DateTime.UtcNow,
                LastUsed = DateTime.MinValue,
                UseCount = 0
            };
            if (await GetGlobalFaqEntryAsync(name).ConfigureAwait(false) != null) return false;
            if (await GetGuildFaqEntryAsync(guildId, name).ConfigureAwait(false) != null) return false;

            var collection = _database.GetCollection<GuildFaqEntry>();
            await collection.InsertOneAsync(lEntry);
            return true;
        }

        public async Task<bool> AddGlobalEntryAsync(ulong userId, string name, string response)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(response)) throw new ArgumentNullException(nameof(response));
            var gEntry = new GlobalFaqEntry
            {
                CreatorId = userId,
                Name = name.ToLower(),
                Content = response,
                CreatedAt = DateTime.UtcNow,
                LastUsed = DateTime.MinValue,
                UseCount = 0
            };
            if (await GetGlobalFaqEntryAsync(name).ConfigureAwait(false) != null) return false;

            var collection = _database.GetCollection<GlobalFaqEntry>();
            await collection.InsertOneAsync(gEntry);
            return true;
        }

        public async Task<DeleteResult> RemoveGuildEntryAsync(GuildFaqEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            return await entry.DeleteAsync();
        }

        public async Task<DeleteResult> RemoveGlobalEntryAsync(GlobalFaqEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            return await entry.DeleteAsync();
        }

        public async Task<ReplaceOneResult> SaveGuildEntryAsync(GuildFaqEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            return await entry.UpdateAsync();
        }

        public async Task<ReplaceOneResult> SaveGlobalEntryAsync(GlobalFaqEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            return await entry.UpdateAsync();
        }

        public async Task<List<GuildFaqEntry>> GetGuildEntriesAsync(ulong guildId)
        {
            return await _database.GetGuildFaqsAsync(guildId);
        }

        public async Task<List<GlobalFaqEntry>> GetGlobalEntriesAsync()
            => await _database.GetGlobalFaqsAsync();

        public async Task<GuildFaqEntry> GetGuildFaqEntryAsync(ulong guildId, string name)
        {
            return await _database.GetGuildFaqAsync(guildId, name);
        }

        public async Task<GlobalFaqEntry> GetGlobalFaqEntryAsync(string name)
            => await _database.GetGlobalFaqAsync(name);

        public async Task<Dictionary<GuildFaqEntry, int>> GetSimilarGuildEntries(ulong guildId, string name)
        {
            var guildEntries = await _database.GetGuildFaqsAsync(guildId);
            var entryDictionary = guildEntries.ToDictionary(guildEntry => guildEntry,
                guildEntry => LevenshteinDistance.Compute(name, guildEntry.Name));
            return entryDictionary.Where(pair => pair.Value <= 4).OrderBy(pair => pair.Value)
                .ToDictionary();
        }

        public async Task<Dictionary<GlobalFaqEntry, int>> GetSimilarGlobalEntries(string name)
        {
            var globalEntries = await _database.GetGlobalFaqsAsync();
            var entryDictionary = globalEntries.ToDictionary(globalEntry => globalEntry,
                globalEntry => LevenshteinDistance.Compute(name, globalEntry.Name));
            return entryDictionary.Where(pair => pair.Value <= 4).OrderBy(pair => pair.Value)
                .ToDictionary();
        }
    }
}