using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Collections;
using Stl.DependencyInjection;

namespace Stl.CommandR.Internal
{
    // This interface just lists all the methods CommandContext has;
    // you should always use CommandContext instead of it.
    internal interface ICommandContext : IHasServices
    {
        ICommander Commander { get; }
        ICommand UntypedCommand { get; }
        Task UntypedResultTask { get; }
        Result<object> UntypedResult { get; set; }
        CommandContext? OuterContext { get; }
        CommandContext OutermostContext { get; }
        CommandExecutionState ExecutionState { get; set; }
        OptionSet Items { get; }

        CommandContext<TResult> Cast<TResult>() => (CommandContext<TResult>) this;
        Task InvokeRemainingHandlersAsync(CancellationToken cancellationToken = default);

        // SetXxx & TrySetXxx
        void SetDefaultResult();
        void SetException(Exception exception);
        void SetCancelled();
        void TrySetDefaultResult();
        void TrySetException(Exception exception);
        void TrySetCancelled(CancellationToken cancellationToken);
    }
}
