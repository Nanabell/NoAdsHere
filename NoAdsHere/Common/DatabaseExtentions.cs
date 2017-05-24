using Discord;
using MongoDB.Bson;
using MongoDB.Driver;
using NoAdsHere.Services;
using NoAdsHere.Services.Penalties;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NoAdsHere.Common
{
    public interface IIndexed
    {
        ObjectId Id { get; set; }
    }

    public static class DatabaseExtentions
    {
        public static IMongoCollection<T> GetCollection<T>(this MongoClient mongo, IDiscordClient client)
        {
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

        public static async Task<GuildSetting> GetGuildAsync(this IMongoCollection<GuildSetting> collection, ulong guildId)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == guildId);
            var result = await cursor.SingleOrDefaultAsync();

            if (result != null)
                return result;
            else
            {
                await collection.InsertOneAsync(new GuildSetting(guildId));

                var cursor2 = await collection.FindAsync((f => f.GuildId == guildId));
                var result2 = await cursor2.SingleOrDefaultAsync();
                return result2;
            }
        }

        public static async Task<Violator> GetUserAsync(this IMongoCollection<Violator> collection, IGuildUser user)
        {
            var cursor = await collection.FindAsync(f => f.GuildId == user.Guild.Id && f.UserId == user.Id);
            var result = await cursor.SingleOrDefaultAsync();

            if (result != null)
                return result;
            else
            {
                await collection.InsertOneAsync(new Violator(user.Guild.Id, user.Id));

                var cursor2 = await collection.FindAsync(f => f.GuildId == user.Guild.Id && f.UserId == user.Id);
                var result2 = await cursor2.SingleOrDefaultAsync();
                return result2;
            }
        }
    }
}