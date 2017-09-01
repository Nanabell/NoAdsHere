using NoAdsHere.Database.Repositories.Interfaces;
using System;

namespace NoAdsHere.Database.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IMasterRepository Masters { get; }
        ISettingsRepository Settings { get; }
        IBlockRepository Blocks { get; }
        IIgnoreRepository Ignores { get; }
        IPenaltyRepository Penalties { get; }
        IViolatorRepository Violators { get; }
        IStatisticRepository Statistics { get; }
        IFaqRepository Faqs { get; }

        int SaveChanges();

        int SaveChanges(bool acceptChangesOnSuccess);
    }
}