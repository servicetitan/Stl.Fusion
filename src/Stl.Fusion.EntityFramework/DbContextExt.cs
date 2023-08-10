using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Stl.Fusion.EntityFramework.Internal;

namespace Stl.Fusion.EntityFramework;

#pragma warning disable EF1001

public static class DbContextExt
{
#if !NETSTANDARD2_0
    private static readonly EventHandler<SavingChangesEventArgs> FailOnSaveChanges =
        (sender, args) => throw Errors.DbContextIsReadOnly();
#endif

    public static TDbContext ReadWrite<TDbContext>(this TDbContext dbContext, bool allowWrites = true)
        where TDbContext : DbContext
        => dbContext
            .EnableChangeTracking(allowWrites)
            .EnableSaveChanges(allowWrites);

    public static TDbContext ReadWrite<TDbContext>(this TDbContext dbContext, bool? allowWrites)
        where TDbContext : DbContext
        => allowWrites is { } vAllowWrites
            ? dbContext.ReadWrite(vAllowWrites)
            : dbContext;

    public static TDbContext EnableChangeTracking<TDbContext>(this TDbContext dbContext, bool mustEnable)
        where TDbContext : DbContext
    {
        var ct = dbContext.ChangeTracker;
        ct.LazyLoadingEnabled = false;
        if (mustEnable) {
            ct.AutoDetectChangesEnabled = true;
            ct.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        }
        else {
            ct.AutoDetectChangesEnabled = false;
            ct.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }
        return dbContext;
    }

    public static TDbContext EnableSaveChanges<TDbContext>(this TDbContext dbContext, bool mustEnable)
        where TDbContext : DbContext
    {
#if !NETSTANDARD2_0
        if (mustEnable)
            dbContext.SavingChanges -= FailOnSaveChanges;
        else
            dbContext.SavingChanges += FailOnSaveChanges;
#else
        // Do nothing. DbContext has no SavingChanges event in NETSTANDARD2_0
#endif
        return dbContext;
    }

    public static TDbContext SuppressExecutionStrategy<TDbContext>(this TDbContext dbContext)
        where TDbContext : DbContext
    {
        ExecutionStrategyExt.Suspend(dbContext);
        return dbContext;
    }

    public static TDbContext SuppressDispose<TDbContext>(this TDbContext dbContext)
        where TDbContext : DbContext
    {
        var dbContextPoolable = (IDbContextPoolable)dbContext;
        dbContextPoolable.SnapshotConfiguration();
        var fakePool = new FakeDbContextPool(dbContextPoolable);
#if !NETSTANDARD2_0
        dbContextPoolable.SetLease(new DbContextLease(fakePool, true));
#else
        dbContextPoolable.SetPool(fakePool);
#endif
        return dbContext;
    }
}
