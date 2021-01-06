using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Internal;

namespace Stl.CommandR.Configuration
{
    public record InterfaceCommandHandler<TCommand> : CommandHandler<TCommand>
        where TCommand : class, ICommand
    {
        public Type HandlerServiceType { get; }

        public InterfaceCommandHandler(Type handlerServiceType, double order = 0)
            : base(order)
        {
            if (!typeof(ICommandHandler<TCommand>).IsAssignableFrom(handlerServiceType))
                throw Errors.MustImplement<ICommandHandler<TCommand>>(handlerServiceType);
            HandlerServiceType = handlerServiceType;
        }

        public override Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var handler = (ICommandHandler<TCommand>) context.GetRequiredService(HandlerServiceType);
            return handler.OnCommandAsync((TCommand) command, context, cancellationToken);
        }
    }

    public static class InterfaceCommandHandler
    {
        public static InterfaceCommandHandler<TCommand> New<TCommand>(
            Type handlerServiceType, double order = 0)
            where TCommand : class, ICommand
            => new(handlerServiceType, order);

        public static InterfaceCommandHandler<TCommand> New<TCommand, THandlerService>(double order = 0)
            where TCommand : class, ICommand
            where THandlerService : ICommandHandler<TCommand>
            => new(typeof(THandlerService), order);

        public static CommandHandler New(
            Type commandType, Type handlerServiceType, double order = 0)
        {
            var ctor = typeof(InterfaceCommandHandler<>)
                .MakeGenericType(commandType)
                .GetConstructors()
                .Single();
            // ReSharper disable once HeapView.BoxingAllocation
            return (CommandHandler) ctor.Invoke(new object[] { handlerServiceType, order });
        }
    }
}
