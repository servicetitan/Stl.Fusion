using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Extensions;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Extensions;

public class DbKeyValueTrimmer<TDbContext, TDbKeyValue> : DbTenantWorkerBase<TDbContext>
    where TDbContext : DbContext
    where TDbKeyValue : DbKeyValue, new()
{
    public record Options
    {
        public RandomTimeSpan CheckPeriod { get; init; } = TimeSpan.FromMinutes(5).ToRandom(0.1);
        public RandomTimeSpan NextBatchDelay { get; init; } = TimeSpan.FromSeconds(1).ToRandom(0.25);
        public RetryDelaySeq RetryDelays { get; init; } = (TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));
        public int BatchSize { get; init; } = 100;
        public LogLevel LogLevel { get; init; } = LogLevel.Information;
    }

    protected Options Settings { get; }
    protected IKeyValueStore KeyValueStore { get; init; }
    protected override IReadOnlyMutableDictionary<Symbol, Tenant> TenantSet => TenantRegistry.AccessedTenants;
    protected bool IsLoggingEnabled { get; }

    public DbKeyValueTrimmer(Options settings, IServiceProvider services)
        : base(services)
    {
        Settings = settings;
        IsLoggingEnabled = Log.IsLogging(Settings.LogLevel);
        KeyValueStore = services.GetRequiredService<IKeyValueStore>();
    }

    protected override Task RunInternal(Tenant tenant, CancellationToken cancellationToken)
    {
        var lastTrimCount = 0;

        var activitySource = GetType().GetActivitySource();
        var runChain = new AsyncChain($"Trim({tenant.Id})", async cancellationToken1 => {
            var dbContext = CreateDbContext(tenant, true);
            await using var _ = dbContext.ConfigureAwait(false);
            dbContext.DisableChangeTracking();

            lastTrimCount = 0;
            var minExpiresAt = Clocks.SystemClock.Now.ToDateTime();
            var keys = await dbContext.Set<TDbKeyValue>().AsQueryable()
                .Where(o => o.ExpiresAt < minExpiresAt)
                .OrderBy(o => o.ExpiresAt)
                .Select(o => o.Key)
                .Take(Settings.BatchSize)
                .ToArrayAsync(cancellationToken1).ConfigureAwait(false);
            if (keys.Length == 0)
                return;

            // This must be done via IKeyValueStore & operations,
            // otherwise invalidation won't happen for removed entries
            await KeyValueStore.Remove(tenant.Id, keys, cancellationToken1).ConfigureAwait(false);
            lastTrimCount = keys.Length;

            if (lastTrimCount > 0 && IsLoggingEnabled)
                Log.Log(Settings.LogLevel,
                    "Trim({tenant.Id}) trimmed {Count} entries", tenant.Id, lastTrimCount);
        }).Trace(() => activitySource.StartActivity("Trim").AddTenantTags(tenant), Log);

        var sleepChain = new AsyncChain("Sleep", cancellationToken1 => {
            var delay = lastTrimCount < Settings.BatchSize
                ? Settings.CheckPeriod
                : Settings.NextBatchDelay;
            return Clocks.CpuClock.Delay(delay.Next(), cancellationToken1);
        });

        var chain = runChain
            .RetryForever(Settings.RetryDelays, Clocks.CpuClock, Log)
            .Append(sleepChain)
            .CycleForever();

        return chain.Start(cancellationToken);
    }
}
