using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Extensions;

namespace Stl.Fusion.EntityFramework.Extensions;

public class DbKeyValueTrimmer<TDbContext, TDbKeyValue> : DbWakeSleepWorkerBase<TDbContext>
    where TDbContext : DbContext
    where TDbKeyValue : DbKeyValue, new()
{
    public class Options
    {
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);
        public int BatchSize { get; set; } = 100;
        public bool IsLoggingEnabled { get; set; } = true;
    }

    protected TimeSpan CheckInterval { get; init; }
    protected int BatchSize { get; init; }
    protected IKeyValueStore KeyValueStore { get; init; }
    protected Random Random { get; init; }

    protected int LastTrimCount { get; set; }
    protected bool IsLoggingEnabled { get; set; }
    protected LogLevel LogLevel { get; set; } = LogLevel.Information;

    public DbKeyValueTrimmer(Options? options, IServiceProvider services)
        : base(services)
    {
        options ??= new();
        IsLoggingEnabled = options.IsLoggingEnabled && Log.IsEnabled(LogLevel);

        CheckInterval = options.CheckInterval;
        BatchSize = options.BatchSize;
        Random = new Random();

        KeyValueStore = services.GetRequiredService<IKeyValueStore>();
    }

    protected override async Task WakeUp(CancellationToken cancellationToken)
    {
        var dbContext = CreateDbContext(true);
        await using var _ = dbContext.ConfigureAwait(false);
        dbContext.DisableChangeTracking();

        LastTrimCount = 0;
        var minExpiresAt = Clocks.SystemClock.Now.ToDateTime();
        var keys = await dbContext.Set<TDbKeyValue>().AsQueryable()
            .Where(o => o.ExpiresAt < minExpiresAt)
            .OrderBy(o => o.ExpiresAt)
            .Select(o => o.Key)
            .Take(BatchSize)
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        if (keys.Length == 0)
            return;

        // This must be done via IKeyValueStore & operations,
        // otherwise invalidation won't happen for removed entries
        await KeyValueStore.RemoveMany(keys, cancellationToken).ConfigureAwait(false);
        LastTrimCount = keys.Length;

        if (LastTrimCount > 0 && IsLoggingEnabled)
            Log.Log(LogLevel, "Trimmed {Count} entries", LastTrimCount);
    }

    protected override Task Sleep(Exception? error, CancellationToken cancellationToken)
    {
        var delay = default(TimeSpan);
        if (error != null)
            delay = TimeSpan.FromMilliseconds(1000 * Random.NextDouble());
        else if (LastTrimCount < BatchSize)
            delay = CheckInterval + TimeSpan.FromMilliseconds(10 * Random.NextDouble());
        return Clocks.CoarseCpuClock.Delay(delay, cancellationToken);
    }
}
