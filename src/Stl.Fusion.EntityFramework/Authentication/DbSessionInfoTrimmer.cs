using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Authentication;

public abstract class DbSessionInfoTrimmer<TDbContext> : DbTenantWorkerBase<TDbContext>
    where TDbContext : DbContext
{
    public record Options
    {
        public RandomTimeSpan CheckPeriod { get; init; } = TimeSpan.FromHours(1).ToRandom(0.1);
        public RandomTimeSpan NextBatchDelay { get; init; } = TimeSpan.FromSeconds(1).ToRandom(0.25);
        public RetryDelaySeq RetryDelays { get; init; } = (TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));
        public TimeSpan MaxSessionAge { get; init; } = TimeSpan.FromDays(60);
        public int BatchSize { get; init; } = 1000;
        public bool IsLoggingEnabled { get; init; } = true;
    }

    protected bool IsLoggingEnabled { get; set; }
    protected LogLevel LogLevel { get; set; } = LogLevel.Information;

    protected Options Settings { get; init; }
    protected override IReadOnlyMutableDictionary<Symbol, Tenant> TenantSet => TenantRegistry.AccessedTenants;

    protected DbSessionInfoTrimmer(Options? options, IServiceProvider services)
        : base(services)
    {
        Settings = options ?? new();
        IsLoggingEnabled = Settings.IsLoggingEnabled && Log.IsEnabled(LogLevel);
    }
}

public class DbSessionInfoTrimmer<TDbContext, TDbSessionInfo, TDbUserId> : DbSessionInfoTrimmer<TDbContext>
    where TDbContext : DbContext
    where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
    where TDbUserId : notnull
{
    protected IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId> Sessions { get; }

    public DbSessionInfoTrimmer(Options? options, IServiceProvider services)
        : base(options, services)
        => Sessions = services.GetRequiredService<IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>>();

    protected override Task RunInternal(Tenant tenant, CancellationToken cancellationToken)
    {
        var lastTrimCount = 0;

        var activitySource = GetType().GetActivitySource();
        var runChain = new AsyncChain($"Trim({tenant.Id})", async cancellationToken1 => {
            var minLastSeenAt = (Clocks.SystemClock.Now - Settings.MaxSessionAge).ToDateTime();
            lastTrimCount = await Sessions
                .Trim(tenant, minLastSeenAt, Settings.BatchSize, cancellationToken)
                .ConfigureAwait(false);

            if (lastTrimCount > 0 && IsLoggingEnabled)
                Log.Log(LogLevel, "Trim({tenant.Id}) trimmed {Count} sessions", tenant.Id, lastTrimCount);
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
