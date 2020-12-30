using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR.Internal;

namespace Stl.CommandR
{
    public interface ICommandDispatcher
    {
        Task<CommandContext> DispatchAsync(ICommand command, CancellationToken cancellationToken = default);
        Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
    }

    public class CommandDispatcher : ICommandDispatcher
    {
        protected IServiceProvider Services { get; }
        protected ICommandHandlerResolver HandlerResolver { get; }
        protected ILogger Log { get; }

        public CommandDispatcher(
            IServiceProvider services,
            ICommandHandlerResolver handlerResolver,
            ILogger<CommandDispatcher>? log = null)
        {
            Log = log ??= NullLogger<CommandDispatcher>.Instance;
            Services = services;
            HandlerResolver = handlerResolver;
        }

        public async Task<CommandContext> DispatchAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            var commandContext = CommandContext.New(command);
            var commandContextImpl = (ICommandContextImpl) commandContext;
            using var _ = commandContext.Activate();
            try {
                var handlers = HandlerResolver.GetCommandHandlers(command.GetType());
                if (handlers.Count == 0) {
                    var error = new InvalidOperationException($"No handler(s) found for {command}.");
                    Log.LogError(error, error.Message);
                    throw error;
                }

                var handlerIndex = 0;
                Func<Task> next = null!;
                next = () => {
                    if (handlerIndex >= handlers!.Count)
                        return Task.CompletedTask;
                    var handler = handlers[handlerIndex++];
                    // ReSharper disable once AccessToModifiedClosure
                    return handler.InvokeAsync(Services, command, next, cancellationToken);
                };
                await next.Invoke().ConfigureAwait(false);
                commandContextImpl.TrySetDefaultResult();
                return commandContext;
            }
            catch (OperationCanceledException) {
                commandContextImpl.TrySetCancelled(
                    cancellationToken.IsCancellationRequested ? cancellationToken : default);
                return commandContext;
            }
            catch (Exception e) {
                commandContextImpl.TrySetException(e);
                return commandContext;
            }
        }

        public async Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
        {
            var commandContext = await DispatchAsync((ICommand) command, cancellationToken).ConfigureAwait(false);
            var typedCommandContext = (CommandContext<TResult>) commandContext;
            return await typedCommandContext.ResultTask.ConfigureAwait(false);
        }
    }
}
