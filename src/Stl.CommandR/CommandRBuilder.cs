using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR.Internal;

namespace Stl.CommandR
{
    public readonly struct CommandRBuilder
    {
        public IServiceCollection Services { get; }
        public ICommandHandlerRegistry Handlers { get; }

        internal CommandRBuilder(IServiceCollection services)
        {
            Services = services;

            Services.TryAddSingleton<ICommandDispatcher, CommandDispatcher>();
            Services.TryAddSingleton<ICommandHandlerRegistry>(new CommandHandlerRegistry());
            Services.TryAddSingleton<ICommandHandlerResolver, CommandHandlerResolver>();

            var handlers = (ICommandHandlerRegistry?) null;
            foreach (var descriptor in Services) {
                if (descriptor.ServiceType == typeof(ICommandHandlerRegistry)) {
                    handlers = (ICommandHandlerRegistry?) descriptor.ImplementationInstance
                        ?? throw Errors.CommandHandlerRegistryMustBeRegisteredAsInstance();
                    break;
                }
            }
            Handlers = handlers ?? throw Errors.CommandHandlerRegistryInstanceIsNotRegistered();
        }

        public CommandRBuilder AddHandler(CommandHandler handler)
        {
            Handlers.AddHandler(handler);
            return this;
        }

        public CommandRBuilder TryAddHandler(CommandHandler handler)
        {
            Handlers.TryAddHandler(handler);
            return this;
        }

        public CommandRBuilder AddHandler<TCommand, THandlerService>(double priority = 0)
            where TCommand : class, ICommand
            where THandlerService : ICommandHandler<TCommand>
            => AddHandler(CommandHandler.New<TCommand, THandlerService>(priority));
        public CommandRBuilder TryAddHandler<TCommand, THandlerService>(double priority = 0)
            where TCommand : class, ICommand
            where THandlerService : ICommandHandler<TCommand>
            => TryAddHandler(CommandHandler.New<TCommand, THandlerService>(priority));
    }
}
