namespace Stl.CommandR.Internal;

// This interface just lists all the methods CommandContext has;
// you should always use CommandContext instead of it.
internal interface ICommandContext : IHasServices
{
    ICommander Commander { get; }
    ICommand UntypedCommand { get; }

    Task UntypedResultTask { get; } // Set at the very end of the pipeline (via Complete)
    Result<object> UntypedResult { get; } // May change while the pipeline runs
    bool IsCompleted { get; }

    CommandContext? OuterContext { get; }
    CommandContext OutermostContext { get; }
    bool IsOutermost { get; }
    CommandExecutionState ExecutionState { get; set; }
    OptionSet Items { get; }

    Task InvokeRemainingHandlers(CancellationToken cancellationToken = default);

    void ResetResult();
    void SetResult(object value);
    void SetResult(Exception exception);

    bool TryComplete(CancellationToken candidateToken);
}
