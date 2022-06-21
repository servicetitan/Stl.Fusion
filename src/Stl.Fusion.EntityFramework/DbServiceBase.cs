using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Multitenancy;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework;

public abstract class DbServiceBase<TDbContext>
    where TDbContext : DbContext
{
    private ILogger? _log;
    private DbHub<TDbContext>? _dbHub;

    protected IServiceProvider Services { get; init; }
    protected DbHub<TDbContext> DbHub => _dbHub ??= Services.DbHub<TDbContext>();
    protected VersionGenerator<long> VersionGenerator => DbHub.VersionGenerator;
    protected MomentClockSet Clocks => DbHub.Clocks;
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    protected DbServiceBase(IServiceProvider services)
        => Services = services;

    protected TDbContext CreateDbContext(bool readWrite = false)
        => DbHub.CreateDbContext(readWrite);
    protected TDbContext CreateDbContext(TenantInfo? tenantInfo, bool readWrite = false)
        => DbHub.CreateDbContext(readWrite);

    protected Task<TDbContext> CreateCommandDbContext(CancellationToken cancellationToken = default)
        => DbHub.CreateCommandDbContext(cancellationToken);
    protected Task<TDbContext> CreateCommandDbContext(TenantInfo? tenantInfo, CancellationToken cancellationToken = default)
        => DbHub.CreateCommandDbContext(tenantInfo, cancellationToken);
}
