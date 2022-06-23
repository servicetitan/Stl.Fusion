using Microsoft.EntityFrameworkCore;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework;

public abstract class DbWakeSleepWorkerBase<TDbContext> : WakeSleepWorkerBase
    where TDbContext : DbContext
{
    private DbHub<TDbContext>? _dbHub;

    protected IServiceProvider Services { get; init; }
    protected DbHub<TDbContext> DbHub => _dbHub ??= Services.DbHub<TDbContext>();
    protected VersionGenerator<long> VersionGenerator => DbHub.VersionGenerator;
    protected MomentClockSet Clocks => DbHub.Clocks;
    protected ICommander Commander => DbHub.Commander;

    protected DbWakeSleepWorkerBase(IServiceProvider services, CancellationTokenSource? stopTokenSource = null)
        : base(stopTokenSource)
    {
        Log = services.LogFor(GetType());
        Services = services;
    }

    protected TDbContext CreateDbContext(bool readWrite = false)
        => DbHub.CreateDbContext(readWrite);
    protected Task<TDbContext> CreateCommandDbContext(CancellationToken cancellationToken = default)
        => DbHub.CreateCommandDbContext(cancellationToken);
    protected Task<TDbContext> CreateCommandDbContext(bool readWrite = true, CancellationToken cancellationToken = default)
        => DbHub.CreateCommandDbContext(readWrite, cancellationToken);
}
