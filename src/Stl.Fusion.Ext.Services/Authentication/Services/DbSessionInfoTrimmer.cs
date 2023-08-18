using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;
using Stl.Multitenancy;

namespace Stl.Fusion.Authentication.Services;

public abstract class DbSessionInfoTrimmer<TDbContext> : DbTenantWorkerBase<TDbContext>
    where TDbContext : DbContext
{
    public record Options
    {
        public RandomTimeSpan CheckPeriod { get; init; } = TimeSpan.FromMinutes(10).ToRandom(0.1);
        public RandomTimeSpan NextBatchDelay { get; init; } = TimeSpan.FromSeconds(0.1).ToRandom(0.25);
        public RetryDelaySeq RetryDelays { get; init; } = RetryDelaySeq.Exp(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));
        public TimeSpan MaxSessionAge { get; init; } = TimeSpan.FromDays(60);
        public int BatchSize { get; init; } = 256;
        public LogLevel LogLevel { get; init; } = LogLevel.Information;
    }

    protected Options Settings { get; }
    protected override IReadOnlyMutableDictionary<Symbol, Tenant> TenantSet => TenantRegistry.AccessedTenants;
    protected bool IsLoggingEnabled { get; }

    protected DbSessionInfoTrimmer(Options settings, IServiceProvider services)
        : base(services)
    {
        Settings = settings;
        IsLoggingEnabled = Log.IsLogging(Settings.LogLevel);
    }
}

public class DbSessionInfoTrimmer<TDbContext, TDbSessionInfo, TDbUserId>(
        DbSessionInfoTrimmer<TDbContext>.Options settings,
        IServiceProvider services
        ) : DbSessionInfoTrimmer<TDbContext>(settings, services)
    where TDbContext : DbContext
    where TDbSessionInfo : DbSessionInfo<TDbUserId>, new()
    where TDbUserId : notnull
{
    protected IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId> Sessions { get; }
        = services.GetRequiredService<IDbSessionInfoRepo<TDbContext, TDbSessionInfo, TDbUserId>>();

    protected override Task RunInternal(Tenant tenant, CancellationToken cancellationToken)
    {
        var lastTrimCount = 0;

        var activitySource = GetType().GetActivitySource();
        var runChain = new AsyncChain($"Trim({tenant.Id})", async cancellationToken1 => {
            var maxLastSeenAt = (Clocks.SystemClock.Now - Settings.MaxSessionAge).ToDateTime();
            lastTrimCount = await Sessions
                .Trim(tenant, maxLastSeenAt, Settings.BatchSize, cancellationToken1)
                .ConfigureAwait(false);

            if (lastTrimCount > 0 && IsLoggingEnabled)
                Log.Log(Settings.LogLevel,
                    "Trim({tenant.Id}) trimmed {Count} sessions", tenant.Id, lastTrimCount);
        }).Trace(() => activitySource.StartActivity("Trim").AddTenantTags(tenant), Log);

        var chain = runChain
            .RetryForever(Settings.RetryDelays, Clocks.CpuClock, Log)
            .AppendDelay(
                () => lastTrimCount < Settings.BatchSize ? Settings.CheckPeriod : Settings.NextBatchDelay,
                Clocks.CpuClock)
            .CycleForever()
            .Log(Log);

        return chain.Start(cancellationToken);
    }
}
