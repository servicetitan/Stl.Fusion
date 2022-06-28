using Stl.Generators;
using Stl.Internal;

namespace Stl.Fusion.Operations.Reprocessing;

/// <summary>
/// Tries to reprocess commands that failed with a reprocessable (transient) error.
/// Must be a transient service.
/// </summary>
public interface IOperationReprocessor : ICommandHandler<ICommand>
{
    public OperationReprocessorOptions Options { get; }
    public IMomentClock DelayClock { get; }
    int FailedTryCount { get; }
    Exception? LastError { get; }

    bool IsTransientFailure(Exception error);
    void AddTransientFailure(Exception error);
    bool WillRetry(Exception error);
}

public record OperationReprocessorOptions
{
    public int MaxRetryCount { get; init; } = 3;
    public RetryDelaySeq RetryDelays { get; init; } =
        new RetryDelaySeq(0.053, 3, 0.5) with { Multiplier = Math.Sqrt(Math.Sqrt(2)) };
    public IMomentClock? DelayClock { get; init; }
}

/// <summary>
/// Tries to reprocess commands that failed with a reprocessable (transient) error.
/// Must be a transient service.
/// </summary>
public class OperationReprocessor : IOperationReprocessor
{
    public static Generator<long> Random { get; } = new RandomInt64Generator();

    protected ILogger Log { get; }
    protected IServiceProvider Services { get; }
    protected IEnumerable<ITransientFailureDetector> TransientFailureDetectors { get; init; }
    protected HashSet<Exception> KnownTransientFailures { get; init; }

    public OperationReprocessorOptions Options { get; }
    public IMomentClock DelayClock { get; }
    public int FailedTryCount { get; protected set; }
    public Exception? LastError { get; protected set; }
    public CommandContext CommandContext { get; protected set; } = null!;

    public OperationReprocessor(
        OperationReprocessorOptions options,
        IServiceProvider services,
        ILogger<OperationReprocessor>? log = null)
    {
        Options = options;
        Log = log ?? NullLogger<OperationReprocessor>.Instance;
        Services = services;
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
        => FailedTryCount <= Options.MaxRetryCount && IsTransientFailure(error);

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
                context.ExecutionState = executionStateBackup;
                context.Items.Items = itemsBackup;
                var delay = Options.RetryDelays[FailedTryCount];
                Log.LogWarning(
                    "Retry #{FailedTryCount}/{MaxTryCount} on {Error}: {Command} with {Delay} delay",
                    FailedTryCount, Options.MaxRetryCount,
                    new ExceptionInfo(error), command, delay.ToShortString());
                await DelayClock.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
