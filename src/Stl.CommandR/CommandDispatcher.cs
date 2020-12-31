using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR.Internal;
using Stl.DependencyInjection;

namespace Stl.CommandR
{
    public interface ICommandDispatcher : IHasServiceProvider
    {
        Task<CommandContext> DispatchAsync(ICommand command, CancellationToken cancellationToken = default);
        Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
    }

    public class CommandDispatcher : ICommandDispatcher
    {
        protected ICommandHandlerResolver HandlerResolver { get; }
        protected ILogger Log { get; }
        public IServiceProvider ServiceProvider { get; }

        public CommandDispatcher(
            IServiceProvider serviceProvider,
            ICommandHandlerResolver handlerResolver,
            ILogger<CommandDispatcher>? log = null)
        {
            Log = log ??= NullLogger<CommandDispatcher>.Instance;
            HandlerResolver = handlerResolver;
            ServiceProvider = serviceProvider;
        }

        public async Task<CommandContext> DispatchAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            var context = CommandContext.New(command, ServiceProvider);
            using var _ = context.Activate();
            try {
                context.Handlers = HandlerResolver.GetCommandHandlers(command.GetType());
                if (context.Handlers.Count == 0) {
                    var error = new InvalidOperationException($"No handler(s) found for {command}.");
                    Log.LogError(error, error.Message);
                    throw error;
                }
                await context.InvokeNextHandlerAsync(cancellationToken).ConfigureAwait(false);
                context.TrySetDefaultResult();
                return context;
            }
            catch (OperationCanceledException) {
                context.TrySetCancelled(
                    cancellationToken.IsCancellationRequested ? cancellationToken : default);
                return context;
            }
            catch (Exception e) {
                context.TrySetException(e);
                return context;
            }
        }

        public async Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        {
            var context = await DispatchAsync((ICommand) command, cancellationToken).ConfigureAwait(false);
            var typedContext = (CommandContext<TResult>) context;
            return await typedContext.ResultTask.ConfigureAwait(false);
        }
    }
}
