using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Collections;
using Stl.Text;
using Stl.Time;

namespace Stl.Fusion.Operations
{
    public interface IOperationCompletionNotifier
    {
        Task<bool> NotifyCompleted(IOperation operation);
    }

    public class OperationCompletionNotifier : IOperationCompletionNotifier
    {
        public class Options
        {
            public int MaxKnownOperationCount { get; set; } = 10_000;
            public TimeSpan MaxKnownOperationAge { get; set; } = TimeSpan.FromHours(1);
            public IMomentClock? Clock { get; set; }
        }

        protected int MaxKnownOperationCount { get; }
        protected TimeSpan MaxKnownOperationAge { get; }
        protected AgentInfo AgentInfo { get; }
        protected IMomentClock Clock { get; }
        protected IOperationCompletionListener[] OperationCompletionListeners { get; }
        protected BinaryHeap<Moment, Symbol> KnownOperationHeap { get; } = new();
        protected HashSet<Symbol> KnownOperationSet { get; } = new();
        protected object Lock { get; } = new();
        protected ILogger Log { get; }

        public OperationCompletionNotifier(Options? options,
            IServiceProvider services,
            ILogger<OperationCompletionNotifier>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<OperationCompletionNotifier>.Instance;
            MaxKnownOperationCount = options.MaxKnownOperationCount;
            MaxKnownOperationAge = options.MaxKnownOperationAge;
            Clock = options.Clock ?? services.SystemClock();
            AgentInfo = services.GetRequiredService<AgentInfo>();
            OperationCompletionListeners = services.GetServices<IOperationCompletionListener>().ToArray();
        }

        public Task<bool> NotifyCompleted(IOperation operation)
        {
            var now = Clock.Now;
            var minOperationStartTime = now - MaxKnownOperationAge;
            var operationStartTime = operation.StartTime.ToMoment();
            var operationId = (Symbol) operation.Id;
            lock (Lock) {
                if (KnownOperationSet.Contains(operationId))
                    return TaskExt.FalseTask;
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
                if (KnownOperationSet.Add(operationId))
                    KnownOperationHeap.Add(operationStartTime, operationId);
            }

            using var _ = ExecutionContextExt.SuppressFlow();
            return Task.Run(async () => {
                var tasks = new Task[OperationCompletionListeners.Length];
                for (var i = 0; i < OperationCompletionListeners.Length; i++) {
                    var handler = OperationCompletionListeners[i];
                    try {
                        tasks[i] = handler.OnOperationCompleted(operation);
                    }
                    catch (Exception e) {
                        Log.LogError(e, "Error in operation completion handler of type '{HandlerType}'",
                            handler.GetType());
                    }
                }
                try {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception e) {
                    Log.LogError(e, "Error in one of operation completion handlers");
                }
                return true;
            });
        }
    }
}
