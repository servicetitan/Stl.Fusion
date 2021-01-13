using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stl.Async;
using Stl.Collections;
using Stl.Time;

namespace Stl.Fusion.EntityFramework
{
    public interface IDbOperationNotifier<TDbContext> : IAsyncProcess
    {
        void NotifyConfirmedOperation(IDbOperation operation);
        event Action<IDbOperation>? ConfirmedOperation;
    }

    public abstract class DbOperationNotifierBase<TDbContext> : DbAsyncProcessBase<TDbContext>, IDbOperationNotifier<TDbContext>
        where TDbContext : DbContext
    {
        public class Options
        {
            public TimeSpan MaxCommitDuration { get; set; } = TimeSpan.FromSeconds(1);
            public int MaxKnownOperationCount { get; set; } = 10_000;
            public TimeSpan MaxKnownOperationAge { get; set; } = TimeSpan.FromHours(1);
        }

        protected TimeSpan MaxCommitDuration { get; }
        protected int MaxKnownOperationCount { get; }
        protected TimeSpan MaxKnownOperationAge { get; }
        protected AgentInfo AgentInfo { get; }
        protected BinaryHeap<Moment, string> KnownOperationHeap { get; } = new();
        protected HashSet<string> KnownOperationSet { get; } = new();
        protected Moment MaxKnownOperationStartTime { get; set; }
        protected object Lock { get; } = new();

        public event Action<IDbOperation>? ConfirmedOperation;

        public DbOperationNotifierBase(Options? options, AgentInfo agentInfo, IServiceProvider services)
            : base(services)
        {
            options ??= new();
            MaxCommitDuration = options.MaxCommitDuration;
            MaxKnownOperationCount = options.MaxKnownOperationCount;
            MaxKnownOperationAge = options.MaxKnownOperationAge;
            AgentInfo = agentInfo;
            MaxKnownOperationStartTime = Clock.Now - MaxCommitDuration;
        }

        public void NotifyConfirmedOperation(IDbOperation operation)
        {
            var now = Clock.Now;
            var minOperationStartTime = now - MaxKnownOperationAge;
            var operationStartTime = operation.StartTime.DefaultKind(DateTimeKind.Utc).ToMoment();
            lock (Lock) {
                if (KnownOperationSet.Contains(operation.Id))
                    return;
                // Removing some operations if there are too many
                while (KnownOperationSet.Count >= MaxKnownOperationCount) {
                    if (KnownOperationHeap.ExtractMin().IsSome(out var value))
                        KnownOperationSet.Remove(value.Value);
                    else
                        break;
                }
                // Removing too old operations
                while (KnownOperationHeap.PeekMin().IsSome(out var value) && value.Priority < minOperationStartTime) {
                    KnownOperationHeap.ExtractMin();
                    KnownOperationSet.Remove(value.Value);
                }
                // Adding the current one
                if (KnownOperationSet.Add(operation.Id)) {
                    KnownOperationHeap.Add(operationStartTime, operation.Id);
                    if (MaxKnownOperationStartTime < operationStartTime)
                        MaxKnownOperationStartTime = operationStartTime;
                }
            }
            using var _ = ExecutionContextEx.SuppressFlow();
            Task.Run(() => {
                try {
                    ConfirmedOperation?.Invoke(operation);
                }
                catch (Exception e) {
                    Log.LogError(e, "Operation notification failed.");
                }
            });
        }

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            for (;;) {
                Moment maxKnownOperationStartTime;
                lock (Lock) {
                    maxKnownOperationStartTime = MaxKnownOperationStartTime;
                }
                try {
                    await FetchNewOperationsAsync(maxKnownOperationStartTime, cancellationToken);
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    Log.LogError(e, "Operation log tail extraction failed.");
                }
                await WaitForNewOperationsAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract Task WaitForNewOperationsAsync(CancellationToken cancellationToken);
        protected abstract Task FetchNewOperationsAsync(
            Moment maxKnownOperationStartTime, CancellationToken cancellationToken);
    }

    public class DbOperationNotifier<TDbContext, TDbOperation> : DbOperationNotifierBase<TDbContext>
        where TDbContext : DbContext
        where TDbOperation : class, IDbOperation, new()
    {
        public new class Options : DbOperationNotifierBase<TDbContext>.Options
        {
            public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(0.1);
        }

        protected TimeSpan CheckInterval { get; }

        public DbOperationNotifier(Options? options, AgentInfo agentInfo, IServiceProvider services)
            : base(options ??= new(), agentInfo, services)
            => CheckInterval = options.CheckInterval;

        protected override Task WaitForNewOperationsAsync(CancellationToken cancellationToken)
            => Clock.DelayAsync(CheckInterval, cancellationToken);

        protected override async Task FetchNewOperationsAsync(
            Moment maxKnownOperationStartTime, CancellationToken cancellationToken)
        {
            var minStartTime = (maxKnownOperationStartTime - MaxCommitDuration).ToDateTime();
            await using var dbContext = CreateDbContext();
            var operations = await dbContext.Set<TDbOperation>().AsQueryable()
                .Where(o => o.StartTime >= minStartTime)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var operation in operations)
                NotifyConfirmedOperation(operation);
        }
    }
}
