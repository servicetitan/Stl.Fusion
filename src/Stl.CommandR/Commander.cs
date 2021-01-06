using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR.Configuration;
using Stl.CommandR.Internal;
using Stl.DependencyInjection;

namespace Stl.CommandR
{
    public interface ICommander : IHasServices
    {
        // This method doesn't throw exceptions
        Task<CommandContext> RunAsync(ICommand command, bool isolate, CancellationToken cancellationToken = default);
        // And this one does
        Task<TResult> CallAsync<TResult>(ICommand<TResult> command, bool isolate, CancellationToken cancellationToken = default);
    }

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

        public async Task<CommandContext> RunAsync(
            ICommand command, bool isolate, CancellationToken cancellationToken = default)
        {
            using var context = CommandContext.New(this, command, isolate);
            var contextImpl = (ICommandContextImpl) context;
            try {
                contextImpl.Handlers = HandlerResolver.GetCommandHandlers(command.GetType());
                if (contextImpl.Handlers.Count == 0)
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

        public async Task<TResult> CallAsync<TResult>(
            ICommand<TResult> command, bool isolate, CancellationToken cancellationToken = default)
        {
            var context = await RunAsync(command, isolate, cancellationToken).ConfigureAwait(false);
            var typedContext = (CommandContext<TResult>) context;
            return await typedContext.ResultTask.ConfigureAwait(false);
        }

        protected virtual Task OnUnhandledCommandAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
            => throw Errors.NoHandlerFound(command);
    }
}
