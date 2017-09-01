using NoAdsHere.Database.Repositories;
using NoAdsHere.Database.Repositories.Interfaces;

namespace NoAdsHere.Database.UnitOfWork
{
    public class NoAdsHereUnit : IUnitOfWork
    {
        private readonly NoAdsHereContext _context;

        public NoAdsHereUnit(NoAdsHereContext context)
        {
            _context = context;
            Masters = new MasterRepository(_context);
            Settings = new SettingsRepository(_context);
            Blocks = new BlockRepository(_context);
            Ignores = new IgnoreRepository(_context);
            Penalties = new PenaltyRepository(_context);
            Violators = new ViolatorRepository(_context);
            Statistics = new StatisticRepository(_context);
            Faqs = new FaqRepository(_context);
        }

        public IMasterRepository Masters { get; }
        public ISettingsRepository Settings { get; }
        public IBlockRepository Blocks { get; }
        public IIgnoreRepository Ignores { get; }
        public IPenaltyRepository Penalties { get; }
        public IViolatorRepository Violators { get; }
        public IStatisticRepository Statistics { get; }
        public IFaqRepository Faqs { get; }

        public void Dispose()
        {
            _context.Dispose();
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public int SaveChanges(bool acceptChangesOnSuccess)
            => _context.SaveChanges(acceptChangesOnSuccess);
    }
}