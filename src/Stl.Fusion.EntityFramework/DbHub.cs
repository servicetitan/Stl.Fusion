using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Internal;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework;

public class DbHub<TDbContext>
    where TDbContext : DbContext
{
    private IDbContextFactory<TDbContext>? _dbContextFactory;
    private VersionGenerator<long>? _versionGenerator;
    private MomentClockSet? _clocks;
    private ICommander? _commander;
    private ILogger? _log;

    protected ILogger Log => _log ??= Services.LogFor(GetType());
    protected IServiceProvider Services { get; }

    public IDbContextFactory<TDbContext> DbContextFactory
        => _dbContextFactory ??= Services.GetRequiredService<IDbContextFactory<TDbContext>>();
    public VersionGenerator<long> VersionGenerator
        => _versionGenerator ??= Services.VersionGenerator<long>();
    public MomentClockSet Clocks
        => _clocks ??= Services.Clocks();
    public ICommander Commander
        => _commander ??= Services.Commander();

    public DbHub(IServiceProvider services)
        => Services = services;

    public TDbContext CreateDbContext(bool readWrite = false)
        => DbContextFactory.CreateDbContext().ReadWrite(readWrite);

    public Task<TDbContext> CreateCommandDbContext(CancellationToken cancellationToken = default)
        => CreateCommandDbContext(true, cancellationToken);
    public Task<TDbContext> CreateCommandDbContext(bool readWrite = true, CancellationToken cancellationToken = default)
    {
        if (Computed.IsInvalidating())
            throw Errors.CreateCommandDbContextIsCalledFromInvalidationCode();

        var commandContext = CommandContext.GetCurrent();
        var operationScope = commandContext.Items.Get<DbOperationScope<TDbContext>>()
            ?? throw new KeyNotFoundException();
        return operationScope.CreateDbContext(readWrite, cancellationToken);
    }
}
