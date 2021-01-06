using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Collections;
using Stl.CommandR.Configuration;

namespace Stl.CommandR.Internal
{
    public interface ICommandContext : IServiceProvider, IDisposable
    {
        ICommander Commander { get; }
        ICommand UntypedCommand { get; }
        Task UntypedResultTask { get; }
        Result<object> UntypedResult { get; set; }
        NamedValueSet Items { get; }
        CommandContext? OuterContext { get; }
        CommandContext OutermostContext { get; }
        IServiceProvider ScopedServices { get; }

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

    public interface ICommandContextImpl : ICommandContext
    {
        IServiceScope ServiceScope { get; }
        IReadOnlyList<CommandHandler> Handlers { get; set; }
        int NextHandlerIndex { get; set; }
    }
}
