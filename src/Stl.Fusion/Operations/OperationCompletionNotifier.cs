using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.Collections;
using Stl.Time;

namespace Stl.Fusion.Operations
{
    public interface IOperationCompletionNotifier
    {
        bool NotifyCompleted(IOperation operation);
    }

    public class OperationCompletionNotifier : IOperationCompletionNotifier
    {
        public class Options
        {
            public int MaxKnownOperationCount { get; set; } = 10_000;
            public TimeSpan MaxKnownOperationAge { get; set; } = TimeSpan.FromHours(1);
        }

        protected int MaxKnownOperationCount { get; }
        protected TimeSpan MaxKnownOperationAge { get; }
        protected AgentInfo AgentInfo { get; }
        protected IOperationCompletionListener[] OperationCompletionHandlers { get; }
        protected IMomentClock Clock { get; }
        protected BinaryHeap<Moment, string> KnownOperationHeap { get; } = new();
        protected HashSet<string> KnownOperationSet { get; } = new();
        protected object Lock { get; } = new();
        protected ILogger Log { get; }

        public OperationCompletionNotifier(Options? options,
            AgentInfo agentInfo,
            IEnumerable<IOperationCompletionListener> operationCompletionHandlers,
            IMomentClock? clock = null,
            ILogger<OperationCompletionNotifier>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<OperationCompletionNotifier>.Instance;
            Clock = clock ?? SystemClock.Instance;
            MaxKnownOperationCount = options.MaxKnownOperationCount;
            MaxKnownOperationAge = options.MaxKnownOperationAge;
            AgentInfo = agentInfo;
            OperationCompletionHandlers = operationCompletionHandlers.ToArray();
        }

        public bool NotifyCompleted(IOperation operation)
        {
            var now = Clock.Now;
            var minOperationStartTime = now - MaxKnownOperationAge;
            var operationStartTime = operation.StartTime.DefaultKind(DateTimeKind.Utc).ToMoment();
            lock (Lock) {
                if (KnownOperationSet.Contains(operation.Id))
                    return false;
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
                if (KnownOperationSet.Add(operation.Id))
                    KnownOperationHeap.Add(operationStartTime, operation.Id);
            }
            using var _ = ExecutionContextEx.SuppressFlow();
            Task.Run(() => {
                foreach (var handler in OperationCompletionHandlers) {
                    try {
                        handler.OnOperationCompleted(operation);
                    }
                    catch (Exception e) {
                        Log.LogError(e, $"Error in operation completion handler of type '{handler.GetType()}'.");
                    }
                }
            });
            return true;
        }
    }
}
