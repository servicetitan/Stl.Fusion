using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.CommandR.Configuration
{
    public record InterfaceCommandHandler<TCommand> : CommandHandler<TCommand>
        where TCommand : class, ICommand
    {
        public Type ServiceType { get; }

        public InterfaceCommandHandler(Type serviceType, double order = 0)
            : base(order)
            => ServiceType = serviceType;

        public override Task InvokeAsync(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var handler = (ICommandHandler<TCommand>) context.GetRequiredService(ServiceType);
            return handler.OnCommandAsync((TCommand) command, context, cancellationToken);
        }
    }

    public static class InterfaceCommandHandler
    {
        public static InterfaceCommandHandler<TCommand> New<TCommand>(
            Type serviceType, double order = 0)
            where TCommand : class, ICommand
            => new(serviceType, order);

        public static InterfaceCommandHandler<TCommand> New<TService, TCommand>(double order = 0)
            where TService : class
            where TCommand : class, ICommand
            => new(typeof(TService), order);

        public static CommandHandler New(
            Type serviceType, Type commandType, double order = 0)
        {
            var ctor = typeof(InterfaceCommandHandler<>)
                .MakeGenericType(commandType)
                .GetConstructors()
                .Single();
            // ReSharper disable once HeapView.BoxingAllocation
            return (CommandHandler) ctor.Invoke(new object[] { serviceType, order });
        }
    }
}
