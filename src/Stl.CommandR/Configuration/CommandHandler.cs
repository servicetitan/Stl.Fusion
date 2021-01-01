using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    public abstract record CommandHandler
    {
        public Type CommandType { get; init; } = null!;
        public double Priority { get; init; }

        public static CommandHandler<TCommand> New<TCommand, THandlerService>(double priority = 0)
            where TCommand : class, ICommand
            where THandlerService : ICommandHandler<TCommand>
            => new() {
                CommandType = typeof(TCommand),
                HandlerServiceType = typeof(THandlerService),
                Priority = priority,
            };

        public abstract Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken);
    }

    public record CommandHandler<TCommand> : CommandHandler
        where TCommand : class, ICommand
    {
        public Type HandlerServiceType { get; init; } = null!;

        public CommandHandler() => CommandType = typeof(TCommand);

        public override Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var services = context.ServiceProvider;
            var handler = (ICommandHandler<TCommand>) services.GetRequiredService(HandlerServiceType);
            return handler.OnCommandAsync((TCommand) command, context, cancellationToken);
        }
    }
}
