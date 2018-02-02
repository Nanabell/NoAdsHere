using Database;

namespace Bot
{
    public class Config
    {
        public string Token { get; set; }
        public int Shards { get; set; }
        public string Prefix { get; set; }
        public DbProviderTypes DatabaseType { get; set; } = DbProviderTypes.SQLite;
        public string ConnectionString { get; set; }
    }
}