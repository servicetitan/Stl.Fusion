using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework;

public abstract class DbTenantWorkerBase<TDbContext> : TenantWorkerBase<TDbContext>
    where TDbContext : DbContext
{
    private ILogger? _log;
    private DbHub<TDbContext>? _dbHub;

    protected IServiceProvider Services { get; init; }
    protected DbHub<TDbContext> DbHub => _dbHub ??= Services.DbHub<TDbContext>();
    protected VersionGenerator<long> VersionGenerator => DbHub.VersionGenerator;
    protected MomentClockSet Clocks => DbHub.Clocks;
    protected ICommander Commander => DbHub.Commander;
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    protected DbTenantWorkerBase(IServiceProvider services, CancellationTokenSource? stopTokenSource = null)
        : base(services.GetRequiredService<ITenantRegistry<TDbContext>>(), stopTokenSource)
        => Services = services;

    protected TDbContext CreateDbContext(bool readWrite = false)
        => DbHub.CreateDbContext(readWrite);
    protected TDbContext CreateDbContext(Symbol tenantId, bool readWrite = false)
        => DbHub.CreateDbContext(tenantId, readWrite);
    protected TDbContext CreateDbContext(Tenant tenant, bool readWrite = false)
        => DbHub.CreateDbContext(tenant, readWrite);

    protected Task<TDbContext> CreateCommandDbContext(CancellationToken cancellationToken = default)
        => DbHub.CreateCommandDbContext(cancellationToken);
    protected Task<TDbContext> CreateCommandDbContext(Symbol tenantId, CancellationToken cancellationToken = default)
        => DbHub.CreateCommandDbContext(tenantId, cancellationToken);
    protected Task<TDbContext> CreateCommandDbContext(Tenant tenant, CancellationToken cancellationToken = default)
        => DbHub.CreateCommandDbContext(tenant, cancellationToken);
}
