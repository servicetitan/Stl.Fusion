using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;
using Stl.CommandR.Configuration;

namespace Stl.CommandR.Internal
{
    public class Commander : ICommander
    {
        protected ICommandHandlerResolver HandlerResolver { get; }
        protected ILogger Log { get; }
        public IServiceProvider Services { get; }

        public Commander(
            IServiceProvider services,
            ILogger<Commander>? log = null)
        {
            Log = log ?? NullLogger<Commander>.Instance;
            Services = services;
            HandlerResolver = services.GetRequiredService<ICommandHandlerResolver>();
        }

        public Task RunAsync(
            CommandContext context, bool isolate,
            CancellationToken cancellationToken = default)
        {
            if (!isolate)
                return RunInternalAsync(context, cancellationToken);

            using var _ = ExecutionContextEx.SuppressFlow();
            return Task.Run(() => RunInternalAsync(context, cancellationToken), default);
        }

        protected virtual async Task RunInternalAsync(
            CommandContext context, CancellationToken cancellationToken = default)
        {
            using var _1 = context;
            using var _2 = context.Activate();
            try {
                var command = context.UntypedCommand;
                var handlers = HandlerResolver.GetCommandHandlers(command.GetType());
                context.ExecutionState = new CommandExecutionState(handlers);
                if (handlers!.Count == 0)
                    await OnUnhandledCommandAsync(command, context, cancellationToken).ConfigureAwait(false);
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                context.TrySetCancelled(
                    cancellationToken.IsCancellationRequested ? cancellationToken : default);
            }
            catch (Exception e) {
                context.TrySetException(e);
            }
        }

        protected virtual Task OnUnhandledCommandAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
            => throw Errors.NoHandlerFound(command.GetType());
    }
}
