using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Versioning;

namespace Stl.Fusion.EntityFramework;

public abstract class DbAsyncProcessBase<TDbContext> : AsyncProcessBase
    where TDbContext : DbContext
{
    private IDbContextFactory<TDbContext>? _dbContextFactory;
    private MomentClockSet? _clocks;
    private VersionGenerator<long>? _versionGenerator;
    private ILogger? _log;

    protected IServiceProvider Services { get; init; }
    protected IDbContextFactory<TDbContext> DbContextFactory => _dbContextFactory
        ??= Services.GetRequiredService<IDbContextFactory<TDbContext>>();
    protected MomentClockSet Clocks => _clocks
        ??= Services.Clocks();
    protected VersionGenerator<long> VersionGenerator => _versionGenerator
        ??= Services.VersionGenerator<long>();
    protected ILogger Log => _log
        ??= Services.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? NullLogger.Instance;

    protected DbAsyncProcessBase(IServiceProvider services)
        => Services = services;

    protected TDbContext CreateDbContext(bool readWrite = false)
        => DbContextFactory.CreateDbContext().ReadWrite(readWrite);

    protected Task<TDbContext> CreateCommandDbContext(CancellationToken cancellationToken = default)
        => CreateCommandDbContext(true, cancellationToken);
    protected Task<TDbContext> CreateCommandDbContext(bool readWrite = true, CancellationToken cancellationToken = default)
    {
        var commandContext = CommandContext.GetCurrent();
        var operationScope = commandContext.Items.Get<DbOperationScope<TDbContext>>()
            ?? throw new KeyNotFoundException();
        return operationScope.CreateDbContext(readWrite, cancellationToken);
    }

    protected object[] ComposeKey(params object[] components)
        => components;
}
