namespace Stl.Fusion.Operations;

public interface IOperationCompletionNotifier
{
    bool IsReady();
    Task<bool> NotifyCompleted(IOperation operation, CommandContext? commandContext);
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
    protected RecentlySeenMap<Symbol, Unit> RecentlySeenMapOperations { get; }
    protected object Lock => RecentlySeenMapOperations;
    protected IMomentClock Clock { get; }
    protected ILogger Log { get; }

    public OperationCompletionNotifier(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Services = services;
        Log = Services.LogFor(GetType());
        Clock = Settings.Clock ?? Services.Clocks().SystemClock;

        AgentInfo = Services.GetRequiredService<AgentInfo>();
        OperationCompletionListeners = Services.GetServices<IOperationCompletionListener>().ToArray();
        RecentlySeenMapOperations = new RecentlySeenMap<Symbol, Unit>(
            Settings.MaxKnownOperationCount,
            Settings.MaxKnownOperationAge,
            Clock);
    }

    public bool IsReady()
        => OperationCompletionListeners.All(x => x.IsReady());

    public Task<bool> NotifyCompleted(IOperation operation, CommandContext? commandContext)
    {
        var operationId = (Symbol) operation.Id;
        lock (Lock) {
            if (!RecentlySeenMapOperations.TryAdd(operationId, operation.StartTime))
                return TaskExt.FalseTask;
        }

        using var _ = ExecutionContextExt.SuppressFlow();
        return Task.Run(async () => {
            // An important assertion
            var isLocal = commandContext != null;
            var isFromLocalAgent = StringComparer.Ordinal.Equals(operation.AgentId, AgentInfo.Id.Value);
            if (isLocal != isFromLocalAgent) {
                if (isFromLocalAgent)
                    Log.LogError("Assertion failed: operation w/o CommandContext originates from local agent");
                else
                    Log.LogError("Assertion failed: operation with CommandContext originates from another agent");
            }

            // Notification
            var tasks = new Task[OperationCompletionListeners.Length];
            for (var i = 0; i < OperationCompletionListeners.Length; i++) {
                var handler = OperationCompletionListeners[i];
                try {
                    tasks[i] = handler.OnOperationCompleted(operation, commandContext);
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
