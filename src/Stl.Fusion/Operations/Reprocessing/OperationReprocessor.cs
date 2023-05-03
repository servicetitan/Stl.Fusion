using Stl.Fusion.Operations.Internal;
using Stl.Generators;
using Errors = Stl.Internal.Errors;

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

    void AddTransientFailure(Exception error);
    bool IsTransientFailure(IEnumerable<Exception> allErrors);
    bool WillRetry(IEnumerable<Exception> allErrors);
}

public record OperationReprocessorOptions
{
    public int MaxRetryCount { get; init; } = 3;
    public RetryDelaySeq RetryDelays { get; init; } = new(0.50, 3, 0.33) { Multiplier = Math.Sqrt(2) };
    public IMomentClock? DelayClock { get; init; }
}

/// <summary>
/// Tries to reprocess commands that failed with a reprocessable (transient) error.
/// Must be a transient service.
/// </summary>
public class OperationReprocessor : IOperationReprocessor
{
    private ITransientErrorDetector<IOperationReprocessor>? _transientErrorDetector;
    private IMomentClock? _delayClock;
    private ILogger? _log;

    protected IServiceProvider Services { get; }
    protected ITransientErrorDetector<IOperationReprocessor> TransientErrorDetector => 
        _transientErrorDetector ??= Services.GetRequiredService<ITransientErrorDetector<IOperationReprocessor>>();
    protected HashSet<Exception> KnownTransientFailures { get; } = new();
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public OperationReprocessorOptions Options { get; }
    public IMomentClock DelayClock => _delayClock ??= Options.DelayClock ?? Services.Clocks().CpuClock;

    public int FailedTryCount { get; protected set; }
    public Exception? LastError { get; protected set; }
    public CommandContext CommandContext { get; protected set; } = null!;

    public OperationReprocessor(OperationReprocessorOptions options, IServiceProvider services)
    {
        Options = options;
        Services = services;
    }

    public void AddTransientFailure(Exception error)
    {
        lock (KnownTransientFailures)
            KnownTransientFailures.Add(error);
    }

    public virtual bool IsTransientFailure(IEnumerable<Exception> allErrors)
    {
        lock (KnownTransientFailures) {
            // ReSharper disable once PossibleMultipleEnumeration
            if (allErrors.Any(KnownTransientFailures.Contains))
                return true;
        }
        // ReSharper disable once PossibleMultipleEnumeration
        foreach (var error in allErrors) {
            if (TransientErrorDetector.IsTransient(error)) {
                lock (KnownTransientFailures)
                    KnownTransientFailures.Add(error);
                return true;
            }
        }
        return false;
    }

    public virtual bool WillRetry(IEnumerable<Exception> allErrors)
    {
        if (FailedTryCount > Options.MaxRetryCount)
            return false;
        var operationScope = CommandContext.Items.Get<IOperationScope>();
        if (operationScope is TransientOperationScope)
            return false;
        return IsTransientFailure(allErrors);
    }

    [CommandFilter(Priority = FusionOperationsCommandHandlerPriority.OperationReprocessor)]
    public virtual async Task OnCommand(ICommand command, CommandContext context, CancellationToken cancellationToken)
    {
        var isReprocessingAllowed =
            context.IsOutermost // Should be a top-level command
            && command is not IMetaCommand // No reprocessing for meta commands
            && !Computed.IsInvalidating();
        if (!isReprocessingAllowed) {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
            return;
        }
        if (CommandContext != null)
            throw Errors.InternalError(
                $"{GetType().GetName()} cannot be used more than once in the same command execution pipeline.");
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
            catch (Exception error) when (error is not OperationCanceledException) {
                if (!this.WillRetry(error))
                    throw;

                LastError = error;
                FailedTryCount++;
                context.Items.Items = itemsBackup;
                context.ExecutionState = executionStateBackup;
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
