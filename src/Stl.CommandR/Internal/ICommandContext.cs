using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Collections;
using Stl.CommandR.Configuration;
using Stl.DependencyInjection;

namespace Stl.CommandR.Internal
{
    // This interface just lists all the methods CommandContext has;
    // you should always use CommandContext instead of it.
    public interface ICommandContext : IServiceProvider, IDisposable
    {
        ICommander Commander { get; }
        ICommand UntypedCommand { get; }
        Task UntypedResultTask { get; }
        Result<object> UntypedResult { get; set; }
        CommandContext? OuterContext { get; }
        CommandContext OutermostContext { get; }
        IReadOnlyList<CommandHandler> Handlers { get; set; }
        int NextHandlerIndex { get; set; }
        IServiceScope ServiceScope { get; }
        NamedValueSet Items { get; }

        CommandContext<TResult> Cast<TResult>() => (CommandContext<TResult>) this;
        Task InvokeNextHandlerAsync(CancellationToken cancellationToken);

        // SetXxx & TrySetXxx
        void SetDefaultResult();
        void SetException(Exception exception);
        void SetCancelled();
        void TrySetDefaultResult();
        void TrySetException(Exception exception);
        void TrySetCancelled(CancellationToken cancellationToken);
    }
}
