using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Operations;
using Stl.Time;

namespace Stl.Fusion.EntityFramework.Internal
{
    public class DbOperationLogWatcher<TDbContext> : DbWakeSleepProcessBase<TDbContext>
        where TDbContext : DbContext
    {
        public class Options
        {
            public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(0.2);
            public TimeSpan MaxCommitDuration { get; set; } = TimeSpan.FromSeconds(1);
        }

        protected IOperationCompletionNotifier OperationCompletionNotifier { get; }
        protected IDbOperationLog<TDbContext> DbOperationLog { get; }
        protected TimeSpan CheckInterval { get; }
        protected TimeSpan MaxCommitDuration { get; }
        protected Moment MaxKnownCommitTime { get; set; }

        public DbOperationLogWatcher(Options? options, IServiceProvider services)
            : base(services)
        {
            options ??= new();
            CheckInterval = options.CheckInterval;
            MaxCommitDuration = options.MaxCommitDuration;
            MaxKnownCommitTime = Clock.Now;
            OperationCompletionNotifier = services.GetRequiredService<IOperationCompletionNotifier>();
            DbOperationLog = services.GetRequiredService<IDbOperationLog<TDbContext>>();
        }

        protected override async Task WakeAsync(CancellationToken cancellationToken)
        {
            var minCommitTime = (MaxKnownCommitTime - MaxCommitDuration).ToDateTime();

            // Fetching potentially new operations
            await using var dbContext = CreateDbContext();
            var operations = await DbOperationLog
                .ListNewlyCommittedAsync(dbContext, minCommitTime, cancellationToken)
                .ConfigureAwait(false);

            // Processing them
            foreach (var operation in operations) {
                if (!OperationCompletionNotifier.NotifyCompleted(operation))
                    continue; // We saw this operation already
                var commitTime = operation.CommitTime.ToMoment();
                if (MaxKnownCommitTime < commitTime)
                    MaxKnownCommitTime = commitTime;
            }
        }

        protected override Task SleepAsync(Exception? error, CancellationToken cancellationToken)
            => Clock.DelayAsync(CheckInterval, cancellationToken);
    }
}
