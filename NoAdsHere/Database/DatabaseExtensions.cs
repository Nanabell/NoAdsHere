using MongoDB.Bson;
using MongoDB.Driver;

namespace NoAdsHere.Database
{
    public interface IIndexed
    {
        // ReSharper disable once UnusedMemberInSuper.Global
        ObjectId Id { get; set; }
    }

    public interface IGuildIndexed : IIndexed
    {
        // ReSharper disable once UnusedMemberInSuper.Global
        ulong GuildId { get; set; }
    }

    public static class DatabaseExtensions
    {
        public static IMongoCollection<T> GetCollection<T>(this IMongoDatabase db)
            => db.GetCollection<T>(typeof(T).Name);
    }
}