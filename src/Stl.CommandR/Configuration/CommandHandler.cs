using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    public abstract record CommandHandler
    {
        public Type CommandType { get; private set; }
        public double Priority { get; init; }

        public static CommandHandler<TCommand> New<TCommand>(
            Type handlerServiceType, double priority)
            where TCommand : class, ICommand
            => new(handlerServiceType) { Priority = priority };
        public static CommandHandler<TCommand> New<TCommand, THandlerService>(double priority)
            where TCommand : class, ICommand
            => new(typeof(THandlerService)) { Priority = priority };

        protected CommandHandler(Type commandType)
            => CommandType = commandType;

        public abstract Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken);
    }

    public record CommandHandler<TCommand> : CommandHandler
        where TCommand : class, ICommand
    {
        public Type HandlerServiceType { get; } = null!;

        public CommandHandler(Type handlerServiceType)
            : base(typeof(TCommand))
            => HandlerServiceType = handlerServiceType;

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
