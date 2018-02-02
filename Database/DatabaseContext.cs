using System.Runtime.InteropServices;
using Database.Entities;
using Database.Entities.GuildConfig;
using Database.Entities.InviteWhitelist;
using Microsoft.EntityFrameworkCore;

namespace Database
{
    public class DatabaseContext : DbContext
    {
        private readonly DbProviderTypes _dbType;
        private readonly string _connectionString;

        public DbSet<GuildConfig> GuildConfigs { get; set; }
        public DbSet<DiscordDbUser> DiscordUsers { get; set; }
        public DbSet<ChannelWhitelist> ChannelWhitelists { get; set; }
        public DbSet<RoleWhitelist> RoleWhitelists { get; set; }
        public DbSet<UserWhitelist> UserWhitelists { get; set; }
        
        public DatabaseContext(DbProviderTypes dbType, string connectionString)
        {
            _dbType = dbType;
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (_dbType)
            {
                case DbProviderTypes.SQLite:
                    optionsBuilder.UseSqlite(_connectionString).UseMemoryCache(null);
                    break;
                    
                case DbProviderTypes.Postgres:
                    break;
            }
        }
    }
}