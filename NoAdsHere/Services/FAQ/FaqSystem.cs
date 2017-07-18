using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using MongoDB.Driver;
using MoreLinq;
using NoAdsHere.Common;
using NoAdsHere.Database.Models.FAQ;
using NoAdsHere.Services.Configuration;
using NoAdsHere.Services.Database;

namespace NoAdsHere.Services.FAQ
{
    public class FaqSystem
    {
        private readonly DatabaseService _database;
        private readonly Config _config;

        public FaqSystem(DatabaseService database, Config config)
        {
            _database = database;
            _config = config;
        }

        public async Task<bool> AddGuildEntryAsync(IGuild guild, IUser user, string name, string response)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(response)) throw new ArgumentNullException(nameof(response));
            var lEntry = new GuildFaqEntry
            {
                GuildId = guild.Id,
                CreatorId = user.Id,
                Name = name.ToLower(),
                Content = response,
                CreatedAt = DateTime.UtcNow,
                LastUsed = DateTime.MinValue,
                UseCount = 0
            };
            if (await GetGlobalFaqEntryAsync(name).ConfigureAwait(false) != null) return false;
            if (await GetGuildFaqEntryAsync(guild, name).ConfigureAwait(false) != null) return false;

            var collection = _database.GetCollection<GuildFaqEntry>();
            await collection.InsertOneAsync(lEntry);
            return true;
        }

        public async Task<bool> AddGlobalEntryAsync(IUser user, string name, string response)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(response)) throw new ArgumentNullException(nameof(response));
            var gEntry = new GlobalFaqEntry
            {
                CreatorId = user.Id,
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

        public async Task<List<GuildFaqEntry>> GetGuildEntriesAsync(IGuild guild)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await _database.GetGuildFaqsAsync(guild.Id);
        }

        public async Task<List<GlobalFaqEntry>> GetGlobalEntriesAsync()
            => await _database.GetGlobalFaqsAsync();

        public async Task<GuildFaqEntry> GetGuildFaqEntryAsync(IGuild guild, string name)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            return await _database.GetGuildFaqAsync(guild.Id, name);
        }

        public async Task<GlobalFaqEntry> GetGlobalFaqEntryAsync(string name)
            => await _database.GetGlobalFaqAsync(name);

        public async Task<Dictionary<GuildFaqEntry, int>> GetSimilarGuildEntries(IGuild guild, string name)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));

            var guildEntries = await _database.GetGuildFaqsAsync(guild.Id);
            var entryDictionary = guildEntries.ToDictionary(guildEntry => guildEntry,
                guildEntry => LevenshteinDistance.Compute(name, guildEntry.Name));
            return entryDictionary.Where(pair => pair.Value <= _config.MaxLevenshteinDistance).OrderBy(pair => pair.Value)
                .ToDictionary();
        }

        public async Task<Dictionary<GlobalFaqEntry, int>> GetSimilarGlobalEntries(string name)
        {
            var globalEntries = await _database.GetGlobalFaqsAsync();
            var entryDictionary = globalEntries.ToDictionary(globalEntry => globalEntry,
                globalEntry => LevenshteinDistance.Compute(name, globalEntry.Name));
            return entryDictionary.Where(pair => pair.Value <= _config.MaxLevenshteinDistance).OrderBy(pair => pair.Value)
                .ToDictionary();
        }
    }
}