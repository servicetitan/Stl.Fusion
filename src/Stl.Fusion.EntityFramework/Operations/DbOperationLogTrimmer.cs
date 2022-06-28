using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Operations;

public class DbOperationLogTrimmer<TDbContext> : DbTenantWorkerBase<TDbContext>
    where TDbContext : DbContext
{
    public record Options
    {
        public RandomTimeSpan CheckPeriod { get; init; } = TimeSpan.FromMinutes(5).ToRandom(0.1);
        public RandomTimeSpan NextBatchDelay { get; init; } = TimeSpan.FromSeconds(1).ToRandom(0.25);
        public RetryDelaySeq RetryDelays { get; init; } = (TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));
        public TimeSpan MaxOperationAge { get; init; } = TimeSpan.FromMinutes(10);
        public int BatchSize { get; init; } = 1000;
        public bool IsLoggingEnabled { get; init; } = true;
    }

    protected bool IsLoggingEnabled { get; set; }
    protected LogLevel LogLevel { get; set; } = LogLevel.Information;

    protected Options Settings { get; init; }
    protected IDbOperationLog<TDbContext> DbOperationLog { get; init; }
    protected override IReadOnlyMutableDictionary<Symbol, Tenant> TenantSet => TenantRegistry.AccessedTenants;

    public DbOperationLogTrimmer(Options? settings, IServiceProvider services)
        : base(services)
    {
        Settings = settings ?? new();
        IsLoggingEnabled = Settings.IsLoggingEnabled && Log.IsEnabled(LogLevel);
        DbOperationLog = services.GetRequiredService<IDbOperationLog<TDbContext>>();
    }

    protected override Task RunInternal(Tenant tenant, CancellationToken cancellationToken)
    {
        var lastTrimCount = 0;

        var activitySource = GetType().GetActivitySource();
        var runChain = new AsyncChain($"Trim({tenant.Id})", async cancellationToken1 => {
            var minCommitTime = (Clocks.SystemClock.Now - Settings.MaxOperationAge).ToDateTime();
            lastTrimCount = await DbOperationLog
                .Trim(tenant, minCommitTime, Settings.BatchSize, cancellationToken1)
                .ConfigureAwait(false);

            if (lastTrimCount > 0 && IsLoggingEnabled)
                Log.Log(LogLevel, "Trim({tenant.Id}) trimmed {Count} operations", tenant.Id, lastTrimCount);
        }).Trace(() => activitySource.StartActivity("Trim")?.AddBaggage("TenantId", tenant.Id.Value), Log);

        var sleepChain = new AsyncChain("Sleep", cancellationToken1 => {
            var delay = lastTrimCount < Settings.BatchSize
                ? Settings.NextBatchDelay
                : Settings.CheckPeriod;
            return Clocks.CpuClock.Delay(delay.Next(), cancellationToken1);
        });
        
        var chain = runChain
            .RetryForever(Settings.RetryDelays, Clocks.CpuClock, Log)
            .Append(sleepChain)
            .Cycle();

        return chain.Start(cancellationToken);
    }
}
