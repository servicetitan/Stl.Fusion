using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    public abstract record CommandHandler
    {
        public Type CommandType { get; }
        public double Priority { get; }

        public static CommandHandler<TCommand> New<TCommand>(
            Type handlerServiceType, double priority = 0)
            where TCommand : class, ICommand
            => new(handlerServiceType, priority);
        public static CommandHandler<TCommand> New<TCommand, THandlerService>(double priority = 0)
            where TCommand : class, ICommand
            => new(typeof(THandlerService), priority);

        public static CommandHandler New(
            Type commandType, Type handlerServiceType, double priority = 0)
        {
            var ctor = typeof(CommandHandler<>)
                .MakeGenericType(commandType)
                .GetConstructors()
                .Single();
            // ReSharper disable once HeapView.BoxingAllocation
            return (CommandHandler) ctor.Invoke(new object[] { handlerServiceType, priority });
        }

        protected CommandHandler(Type commandType, double priority = 0)
        {
            CommandType = commandType;
            Priority = priority;
        }

        public abstract Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken);
    }

    public record CommandHandler<TCommand> : CommandHandler
        where TCommand : class, ICommand
    {
        public Type HandlerServiceType { get; } = null!;

        public CommandHandler(Type handlerServiceType, double priority = 0)
            : base(typeof(TCommand), priority)
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
