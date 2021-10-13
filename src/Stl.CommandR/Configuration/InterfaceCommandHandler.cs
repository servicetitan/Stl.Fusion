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

        public InterfaceCommandHandler(Type serviceType, bool isFilter = false, double priority = 0)
            : base(isFilter, priority)
            => ServiceType = serviceType;

        public override object GetHandlerService(ICommand command, CommandContext context)
            => context.Services.GetRequiredService(ServiceType);

        public override Task Invoke(
            ICommand command, CommandContext context,
            CancellationToken cancellationToken)
        {
            var handler = (ICommandHandler<TCommand>) GetHandlerService(command, context);
            return handler.OnCommand((TCommand) command, context, cancellationToken);
        }
    }

    public static class InterfaceCommandHandler
    {
        public static InterfaceCommandHandler<TCommand> New<TCommand>(
            Type serviceType, bool isFilter, double priority = 0)
            where TCommand : class, ICommand
            => new(serviceType, isFilter, priority);

        public static InterfaceCommandHandler<TCommand> New<TService, TCommand>(
            bool isFilter = false, double priority = 0)
            where TService : class
            where TCommand : class, ICommand
            => new(typeof(TService), isFilter, priority);

        public static CommandHandler New(
            Type serviceType, Type commandType, bool isFilter = false, double priority = 0)
        {
            var ctor = typeof(InterfaceCommandHandler<>)
                .MakeGenericType(commandType)
                .GetConstructors()
                .Single();
            // ReSharper disable once HeapView.BoxingAllocation
            return (CommandHandler) ctor.Invoke(new object[] { serviceType, isFilter, priority });
        }
    }
}
