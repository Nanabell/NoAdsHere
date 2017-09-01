using Microsoft.EntityFrameworkCore;
using NoAdsHere.Database.Entities.Global;
using NoAdsHere.Database.Entities.Guild;

namespace NoAdsHere.Database
{
    public class NoAdsHereContext : DbContext
    {
        public DbSet<Master> Masters { get; set; }
        public DbSet<Block> Blocks { get; set; }
        public DbSet<Ignore> Ignores { get; set; }
        public DbSet<Penalty> Penalties { get; set; }
        public DbSet<Violator> Violators { get; set; }
        public DbSet<Statistic> Statistics { get; set; }
        public DbSet<Faq> Faqs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source=NoAdsHere.db").UseMemoryCache(null);
        }
    }
}