namespace Stl.Fusion.Operations;

public interface IOperationCompletionNotifier
{
    bool IsReady();
    Task<bool> NotifyCompleted(IOperation operation);
}

public class OperationCompletionNotifier : IOperationCompletionNotifier
{
    public record Options
    {
        public int MaxKnownOperationCount { get; init; } = 10_000;
        public TimeSpan MaxKnownOperationAge { get; init; } = TimeSpan.FromHours(1);
        public IMomentClock? Clock { get; init; }
    }

    protected Options Settings { get; }
    protected IServiceProvider Services { get; }
    protected AgentInfo AgentInfo { get; }
    protected IOperationCompletionListener[] OperationCompletionListeners { get; }
    protected BinaryHeap<Moment, Symbol> KnownOperationHeap { get; } = new();
    protected HashSet<Symbol> KnownOperationSet { get; } = new();
    protected object Lock { get; } = new();
    protected IMomentClock Clock { get; }
    protected ILogger Log { get; }

    public OperationCompletionNotifier(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
        Log = Services.LogFor(GetType());
        Clock = Settings.Clock ?? Services.SystemClock();

        AgentInfo = Services.GetRequiredService<AgentInfo>();
        OperationCompletionListeners = Services.GetServices<IOperationCompletionListener>().ToArray();
    }

    public bool IsReady() 
        => OperationCompletionListeners.All(x => x.IsReady());

    public Task<bool> NotifyCompleted(IOperation operation)
    {
        var now = Clock.Now;
        var minOperationStartTime = now - Settings.MaxKnownOperationAge;
        var operationStartTime = operation.StartTime.ToMoment();
        var operationId = (Symbol) operation.Id;
        lock (Lock) {
            if (KnownOperationSet.Contains(operationId))
                return TaskExt.FalseTask;
            // Removing some operations if there are too many
            while (KnownOperationSet.Count >= Settings.MaxKnownOperationCount) {
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
