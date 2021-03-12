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
            public int BatchSize { get; set; } = 256;
            public TimeSpan ErrorDelay { get; set; } = TimeSpan.FromSeconds(0.25);
        }

        protected TimeSpan MaxCommitDuration { get; }
        protected TimeSpan UnconditionalWakeUpPeriod { get; }
        protected int BatchSize { get; }
        protected TimeSpan ErrorDelay { get; }

        protected AgentInfo AgentInfo { get; }
        protected IOperationCompletionNotifier OperationCompletionNotifier { get; }
        protected IDbOperationLogChangeTracker<TDbContext>? OperationLogChangeMonitor { get; }
        protected IDbOperationLog<TDbContext> DbOperationLog { get; }
        protected Moment MaxKnownCommitTime { get; set; }
        protected int LastCount { get; set; }

        public DbOperationLogReader(Options? options, IServiceProvider services)
            : base(services)
        {
            options ??= new();
            MaxCommitDuration = options.MaxCommitDuration;
            UnconditionalWakeUpPeriod = options.UnconditionalWakeUpPeriod;
            BatchSize = options.BatchSize;
            ErrorDelay = options.ErrorDelay;

            MaxKnownCommitTime = Clock.Now;
            AgentInfo = services.GetRequiredService<AgentInfo>();
            OperationLogChangeMonitor = services.GetService<IDbOperationLogChangeTracker<TDbContext>>();
            OperationCompletionNotifier = services.GetRequiredService<IOperationCompletionNotifier>();
            DbOperationLog = services.GetRequiredService<IDbOperationLog<TDbContext>>();
        }

        protected override async Task WakeUp(CancellationToken cancellationToken)
        {
            var minCommitTime = (MaxKnownCommitTime - MaxCommitDuration).ToDateTime();

            // Fetching potentially new operations
            var operations = await DbOperationLog
                .ListNewlyCommitted(minCommitTime, BatchSize, cancellationToken)
                .ConfigureAwait(false);

            // Processing them
            var tasks = new Task[operations.Count];
            for (var i = 0; i < operations.Count; i++) {
                var operation = operations[i];
                var isLocal = operation.AgentId == AgentInfo.Id.Value;
                // Local completions are invoked by TransientOperationScopeProvider
                // _inside_ the command processing pipeline. Trying to trigger them here
                // means a tiny chance of running them _outside_ of command processing
                // pipeline, which makes it possible to see command completing
                // prior to its invalidation logic completion.
                tasks[i] = isLocal
                    ? Task.CompletedTask // Skips local operation!
                    : OperationCompletionNotifier.NotifyCompleted(operation);
                var commitTime = operation.CommitTime.ToMoment();
                if (MaxKnownCommitTime < commitTime)
                    MaxKnownCommitTime = commitTime;
            }

            // Let's wait when all of these tasks complete, otherwise
            // we might end up creating too many tasks
            await Task.WhenAll(tasks).ConfigureAwait(false);
            LastCount = operations.Count;
        }

        protected override Task Sleep(Exception? error, CancellationToken cancellationToken)
        {
            if (error != null)
                return Clock.Delay(ErrorDelay, cancellationToken);
            if (LastCount == BatchSize)
                return Task.CompletedTask;
            if (OperationLogChangeMonitor == null)
                return Clock.Delay(UnconditionalWakeUpPeriod, cancellationToken);
            return OperationLogChangeMonitor
                .WaitForChanges(cancellationToken)
                .WithTimeout(Clock, UnconditionalWakeUpPeriod, cancellationToken);
        }
    }
}
