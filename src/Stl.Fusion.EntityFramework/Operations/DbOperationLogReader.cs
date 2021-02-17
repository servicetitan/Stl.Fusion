using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Async;
using Stl.Fusion.Operations;
using Stl.Time;

namespace Stl.Fusion.EntityFramework.Operations
{
    public class DbOperationLogReader<TDbContext> : DbWakeSleepProcessBase<TDbContext>
        where TDbContext : DbContext
    {
        public class Options
        {
            public TimeSpan MaxCommitDuration { get; set; } = TimeSpan.FromSeconds(1);
            public TimeSpan UnconditionalWakeUpPeriod { get; set; } = TimeSpan.FromSeconds(0.25);
        }

        protected IOperationCompletionNotifier OperationCompletionNotifier { get; }
        protected IDbOperationLog<TDbContext> DbOperationLog { get; }
        protected TimeSpan MaxCommitDuration { get; }
        protected TimeSpan UnconditionalWakeUpPeriod { get; }
        protected IDbOperationLogChangeTracker<TDbContext>? OperationLogChangeMonitor { get; }
        protected Moment MaxKnownCommitTime { get; set; }

        public DbOperationLogReader(Options? options,
            IServiceProvider services,
            IDbOperationLogChangeTracker<TDbContext>? operationLogChangeMonitor = null)
            : base(services)
        {
            options ??= new();
            MaxCommitDuration = options.MaxCommitDuration;
            UnconditionalWakeUpPeriod = options.UnconditionalWakeUpPeriod;
            MaxKnownCommitTime = Clock.Now;
            OperationLogChangeMonitor = operationLogChangeMonitor;
            OperationCompletionNotifier = services.GetRequiredService<IOperationCompletionNotifier>();
            DbOperationLog = services.GetRequiredService<IDbOperationLog<TDbContext>>();
        }

        protected override async Task WakeUpAsync(CancellationToken cancellationToken)
        {
            var minCommitTime = (MaxKnownCommitTime - MaxCommitDuration).ToDateTime();

            // Fetching potentially new operations
            var operations = await DbOperationLog
                .ListNewlyCommittedAsync(minCommitTime, cancellationToken)
                .ConfigureAwait(false);

            // var secondsAgo = (Clock.Now.ToDateTime() - minCommitTime).TotalSeconds;
            // Log.LogInformation("({Ago:F2}s ago ... now): {OpCount} operations",
            //     secondsAgo, operations.Count);

            // Processing them
            foreach (var operation in operations) {
                OperationCompletionNotifier.NotifyCompleted(operation);
                var commitTime = operation.CommitTime.ToMoment();
                if (MaxKnownCommitTime < commitTime)
                    // This update should happen even for locally executed operations,
                    // i.e. when NotifyCompleted(...) returns false!
                    MaxKnownCommitTime = commitTime;
            }
        }

        protected override Task SleepAsync(Exception? error, CancellationToken cancellationToken)
            => OperationLogChangeMonitor?.WaitForChangesAsync(cancellationToken)
                .WithTimeout(Clock, UnconditionalWakeUpPeriod, cancellationToken)
            ?? Clock.DelayAsync(UnconditionalWakeUpPeriod, cancellationToken);
    }
}
