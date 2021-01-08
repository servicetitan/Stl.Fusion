using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Collections;
using Stl.CommandR.Configuration;
using Stl.CommandR.Internal;

namespace Stl.CommandR
{
    public readonly struct CommanderBuilder
    {
        public IServiceCollection Services { get; }
        public ICommandHandlerRegistry Handlers { get; }

        internal CommanderBuilder(IServiceCollection services)
        {
            Services = services;

            Services.TryAddSingleton<ICommander, Commander>();
            Services.TryAddSingleton<ICommandHandlerRegistry>(new CommandHandlerRegistry());
            Services.TryAddSingleton<ICommandHandlerResolver, CommandHandlerResolver>();
            services.TryAddScoped<NamedValueSet>();

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

        public CommanderBuilder AddHandler<TCommand, THandlerService>(double order = 0)
            where TCommand : class, ICommand
            where THandlerService : ICommandHandler<TCommand>
            => AddHandler(InterfaceCommandHandler.New<TCommand, THandlerService>(order));

        public CommanderBuilder AddHandler(MethodInfo handlerMethod, double? priorityOverride = null)
            => AddHandler(MethodCommandHandler.New(handlerMethod, priorityOverride));

        // Handler discovery

        public CommanderBuilder AddHandlers<TService>(double? priorityOverride = null)
            => AddHandlers(typeof(TService), priorityOverride);
        public CommanderBuilder AddHandlers(Type serviceType, double? priorityOverride = null)
        {
            // Methods
            var methods = serviceType.GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods) {
                var handler = MethodCommandHandler.TryNew(method, priorityOverride);
                if (handler == null)
                    continue;
                AddHandler(handler);
            }

            // Interfaces
            foreach (var tInterface in serviceType.GetInterfaces()) {
                if (!tInterface.IsGenericType)
                    continue;
                var gInterface = tInterface.GetGenericTypeDefinition();
                if (gInterface != typeof(ICommandHandler<>))
                    continue;
                var tCommand = tInterface.GetGenericArguments().SingleOrDefault();
                if (tCommand == null)
                    continue;
                AddHandler(InterfaceCommandHandler.New(tCommand, serviceType, priorityOverride ?? 0));
            }

            return this;
        }

        // Low-level methods

        public CommanderBuilder AddHandler(CommandHandler handler)
        {
            Handlers.Add(handler);
            return this;
        }

        public CommanderBuilder ClearHandlers()
        {
            Handlers.Clear();
            return this;
        }
    }
}
