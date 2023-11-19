using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Stl.Multitenancy;

namespace Stl.Fusion.EntityFramework.Operations;

public class DbOperationLogTrimmer<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDbContext>
    : DbTenantWorkerBase<TDbContext>
    where TDbContext : DbContext
{
    public record Options
    {
        public RandomTimeSpan CheckPeriod { get; init; } = TimeSpan.FromMinutes(5).ToRandom(0.1);
        public RandomTimeSpan NextBatchDelay { get; init; } = TimeSpan.FromSeconds(1).ToRandom(0.25);
        public RetryDelaySeq RetryDelays { get; init; } = RetryDelaySeq.Exp(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(10));
        public TimeSpan MaxOperationAge { get; init; } = TimeSpan.FromMinutes(10);
        public int BatchSize { get; init; } = 1000;
        public LogLevel LogLevel { get; init; } = LogLevel.Information;
    }

    protected Options Settings { get; }
    protected IDbOperationLog<TDbContext> DbOperationLog { get; init; }
    protected override IReadOnlyMutableDictionary<Symbol, Tenant> TenantSet => TenantRegistry.AccessedTenants;
    protected bool IsLoggingEnabled { get; set; }

    public DbOperationLogTrimmer(Options settings, IServiceProvider services)
        : base(services)
    {
        Settings = settings;
        IsLoggingEnabled = Log.IsLogging(settings.LogLevel);
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
                Log.Log(Settings.LogLevel,
                    "Trim({TenantId}) trimmed {Count} operations", tenant.Id, lastTrimCount);
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
