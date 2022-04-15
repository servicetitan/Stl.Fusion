using Stl.Generators;
using Stl.Internal;

namespace Stl.Fusion.Operations.Reprocessing;

/// <summary>
/// Tries to reprocess commands that failed with a reprocessable (transient) error.
/// Must be a transient service.
/// </summary>
public interface IOperationReprocessor : ICommandHandler<ICommand>
{
    int MaxTryCount { get; }
    int FailedTryCount { get; }
    Exception? LastError { get; }

    bool IsTransientFailure(Exception error);
    void AddTransientFailure(Exception error);
    bool WillRetry(Exception error);
}

/// <summary>
/// Tries to reprocess commands that failed with a reprocessable (transient) error.
/// Must be a transient service.
/// </summary>
public class OperationReprocessor : IOperationReprocessor
{
    public class Options
    {
        public int MaxTryCount { get; set; } = 3;
        public Func<Exception, TimeSpan> RetryDelayProvider { get; set; } =
            _ => TimeSpan.FromMilliseconds(53 + Math.Abs(Random.Next() % 198));
        public IMomentClock? DelayClock { get; set; }
    }

    public static Generator<long> Random { get; } = new RandomInt64Generator();

    public int MaxTryCount { get; init; }
    public int FailedTryCount { get; protected set; }
    public Exception? LastError { get; protected set; }
    public CommandContext CommandContext { get; protected set; } = null!;

    protected Func<Exception, TimeSpan> RetryDelayProvider { get; init; }
    protected IEnumerable<ITransientFailureDetector> TransientFailureDetectors { get; init; }
    protected HashSet<Exception> KnownTransientFailures { get; init; }
    protected IMomentClock DelayClock { get; init; }
    protected IServiceProvider Services { get; init; }
    protected ILogger Log { get; init; }

    public OperationReprocessor(
        Options? options,
        IServiceProvider services,
        ILogger<OperationReprocessor>? log = null)
    {
        options ??= new();
        Log = log ?? NullLogger<OperationReprocessor>.Instance;
        Services = services;
        MaxTryCount = options.MaxTryCount;
        RetryDelayProvider = options.RetryDelayProvider;
        DelayClock = options.DelayClock ?? services.Clocks().CpuClock;
        TransientFailureDetectors = services.GetServices<ITransientFailureDetector>();
        KnownTransientFailures = new();
    }

    public virtual bool IsTransientFailure(Exception error)
    {
        lock (KnownTransientFailures) {
            if (KnownTransientFailures.Contains(error))
                return true;
        }
        foreach (var detector in TransientFailureDetectors) {
            if (detector.IsTransient(error)) {
                lock (KnownTransientFailures)
                    KnownTransientFailures.Add(error);
                return true;
            }
        }
        return false;
    }

    public void AddTransientFailure(Exception error)
    {
        lock (KnownTransientFailures)
            KnownTransientFailures.Add(error);
    }

    public virtual bool WillRetry(Exception error)
        => FailedTryCount + 1 < MaxTryCount && IsTransientFailure(error);

    [CommandHandler(Priority = 100_000, IsFilter = true)]
    public virtual async Task OnCommand(ICommand command, CommandContext context, CancellationToken cancellationToken)
    {
        var reprocessingAllowed =
            context.OuterContext == null // Should be a top-level command
            && !(command is IMetaCommand) // No reprocessing for meta commands
            && !Computed.IsInvalidating();
        if (!reprocessingAllowed) {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
            return;
        }
        if (CommandContext != null)
            throw Errors.InternalError(
                $"{GetType().Name} cannot be used more than once in the same command execution pipeline.");
        CommandContext = context;

        context.Items.Set((IOperationReprocessor) this);
        var itemsBackup = context.Items.Items;
        var executionStateBackup = context.ExecutionState;
        while (true) {
            try {
                await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
                LastError = null;
                break;
            }
            catch (Exception error) {
                if (!WillRetry(error))
                    throw;
                LastError = error;
                FailedTryCount++;
                var delay = RetryDelayProvider(error);
                Log.LogWarning(
                    "Retry #{TryCount}/{MaxTryCount} on {Error}: {Command} with {Delay}ms delay",
                    FailedTryCount + 1, MaxTryCount, new ExceptionInfo(error), command, delay.TotalMilliseconds);
                context.ExecutionState = executionStateBackup;
                context.Items.Items = itemsBackup;
            }
        }
    }
}
