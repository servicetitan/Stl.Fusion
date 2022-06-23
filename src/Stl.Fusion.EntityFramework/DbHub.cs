using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Fusion.EntityFramework.Multitenancy;
using Stl.Fusion.Multitenancy;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework;

public class DbHub<TDbContext>
    where TDbContext : DbContext
{
    private IMultitenantDbContextFactory<TDbContext>? _dbContextFactory;
    private MomentClockSet? _clocks;
    private VersionGenerator<long>? _versionGenerator;
    private ILogger? _log;

    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected IServiceProvider Services { get; }

    public IMultitenantDbContextFactory<TDbContext> DbContextFactory
        => _dbContextFactory ??= Services.GetRequiredService<IMultitenantDbContextFactory<TDbContext>>();
    public MomentClockSet Clocks
        => _clocks ??= Services.Clocks();
    public VersionGenerator<long> VersionGenerator
        => _versionGenerator ??= Services.VersionGenerator<long>();

    public DbHub(IServiceProvider services)
        => Services = services;

    public TDbContext CreateDbContext(bool readWrite = false)
        => DbContextFactory.CreateDbContext(tenantInfo: null).ReadWrite(readWrite);
    public TDbContext CreateDbContext(TenantInfo? tenantInfo, bool readWrite = false)
        => DbContextFactory.CreateDbContext(tenantInfo).ReadWrite(readWrite);

    public Task<TDbContext> CreateCommandDbContext(CancellationToken cancellationToken = default)
        => CreateCommandDbContext(tenantInfo: null, cancellationToken);
    public Task<TDbContext> CreateCommandDbContext(TenantInfo? tenantInfo, CancellationToken cancellationToken = default)
    {
        if (Computed.IsInvalidating())
            throw Errors.CreateCommandDbContextIsCalledFromInvalidationCode();

        var commandContext = CommandContext.GetCurrent();
        var operationScope = commandContext.Items.Get<DbOperationScope<TDbContext>>()
            ?? throw new KeyNotFoundException();
        operationScope.TenantInfo = tenantInfo;
        return operationScope.CreateDbContext(readWrite: true, cancellationToken);
    }
}
