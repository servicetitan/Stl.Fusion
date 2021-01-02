using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR.Configuration;
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

        // Manually add handlers

        public CommandRBuilder AddHandler<TCommand, THandlerService>(double priority = 0)
            where TCommand : class, ICommand
            where THandlerService : ICommandHandler<TCommand>
            => AddHandler(CommandHandler.New<TCommand, THandlerService>(priority));

        public CommandRBuilder AddHandler(MethodInfo handlerMethod, double? priorityOverride = null)
            => AddHandler(MethodCommandHandler.New(handlerMethod, priorityOverride));

        // Handler discovery

        public CommandRBuilder AddHandlers<TService>(double? priorityOverride = null)
            => AddHandlers(typeof(TService), priorityOverride);
        public CommandRBuilder AddHandlers(Type serviceType, double? priorityOverride = null)
        {
            var commandTypes = new HashSet<Type>();

            // Methods
            var methods = serviceType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods) {
                var handler = MethodCommandHandler.TryNew(method, priorityOverride);
                if (handler == null || !commandTypes.Add(handler.CommandType))
                    continue;
                TryAddHandler(handler);
            }

            // Interfaces
            foreach (var tInterface in serviceType.GetInterfaces()) {
                if (!tInterface.IsGenericType)
                    continue;
                var gInterface = tInterface.GetGenericTypeDefinition();
                if (gInterface != typeof(ICommandHandler<>))
                    continue;
                var tCommand = tInterface.GetGenericArguments().SingleOrDefault();
                if (tCommand == null || !commandTypes.Add(tCommand))
                    continue;
                TryAddHandler(CommandHandler.New(tCommand, serviceType, priorityOverride ?? 0));
            }

            return this;
        }

        // Low-level methods

        public CommandRBuilder AddHandler(CommandHandler handler)
        {
            Handlers.Add(handler);
            return this;
        }

        public CommandRBuilder TryAddHandler(CommandHandler handler)
        {
            Handlers.TryAdd(handler);
            return this;
        }

        public CommandRBuilder ClearHandlers()
        {
            Handlers.Clear();
            return this;
        }
    }
}
