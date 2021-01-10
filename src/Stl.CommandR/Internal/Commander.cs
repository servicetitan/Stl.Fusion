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
            Log = log ??= NullLogger<Commander>.Instance;
            Services = services;
            HandlerResolver = services.GetRequiredService<ICommandHandlerResolver>();
        }

        public CommandContext Start(ICommand command, bool isolate, CancellationToken cancellationToken = default)
        {
            CommandContext context = null!;
            CommandContext ContextFactory() {
                context = CommandContext.New(this, command, isolate);
                return context;
            }
            RunAsync(ContextFactory, command, cancellationToken).Ignore();
            return context;
        }

        public Task<CommandContext> RunAsync(ICommand command, bool isolate, CancellationToken cancellationToken = default)
        {
            CommandContext? context;
            CommandContext ContextFactory() {
                context = CommandContext.New(this, command, isolate);
                return context;
            }
            return RunAsync(ContextFactory, command, cancellationToken);
        }

        protected virtual async Task<CommandContext> RunAsync(
            Func<CommandContext> contextFactory, ICommand command,
            CancellationToken cancellationToken = default)
        {
            using var context = contextFactory.Invoke();
            try {
                context.Handlers = HandlerResolver.GetCommandHandlers(command.GetType());
                if (context.Handlers.Count == 0)
                    await OnUnhandledCommandAsync(command, context, cancellationToken).ConfigureAwait(false);
                else
                    await context.InvokeNextHandlerAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                context.TrySetCancelled(
                    cancellationToken.IsCancellationRequested ? cancellationToken : default);
            }
            catch (Exception e) {
                context.TrySetException(e);
            }
            return context;
        }

        protected virtual Task OnUnhandledCommandAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
            => throw Errors.NoHandlerFound(command.GetType());
    }
}
