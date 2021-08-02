using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR.Configuration;
using Stl.CommandR.Interception;
using Stl.CommandR.Internal;
using Stl.DependencyInjection;

namespace Stl.CommandR
{
    public readonly struct CommanderBuilder
    {
        private class AddedTag { }
        private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

        public IServiceCollection Services { get; }
        public ICommandHandlerRegistry Handlers { get; }

        internal CommanderBuilder(IServiceCollection services)
        {
            Services = services;
            if (Services.Contains(AddedTagDescriptor)) {
                Handlers = TryGetCommandHandlerRegistry(services)
                    ?? throw Errors.CommandHandlerRegistryInstanceIsNotRegistered();
                return;
            }

            // We want above Contains call to run in O(1), so...
            Services.Insert(0, AddedTagDescriptor);

            // Common services
            Services.TryAddSingleton<ICommander, Commander>();
            Services.TryAddSingleton<ICommandHandlerRegistry>(new CommandHandlerRegistry());
            Services.TryAddSingleton<ICommandHandlerResolver, CommandHandlerResolver>();
            Services.TryAddTransient(_ => CommandContext.Current!);

            // Command services & their dependencies
            Services.TryAddSingleton(_ => CommandServiceProxyGenerator.Default);
            Services.TryAddSingleton(new CommandServiceInterceptor.Options());
            Services.TryAddSingleton<CommandServiceInterceptor>();
            Services.TryAddSingleton(c => new [] { c.GetRequiredService<CommandServiceInterceptor>() });

            Handlers = TryGetCommandHandlerRegistry(services)
                ?? throw Errors.CommandHandlerRegistryInstanceIsNotRegistered();

            // Default handlers
            Services.AddSingleton<PreparedCommandHandler>();
            AddHandlers<PreparedCommandHandler>();
            Services.AddSingleton<LocalCommandHandler>();
            AddHandlers<LocalCommandHandler>();
        }

        private static ICommandHandlerRegistry? TryGetCommandHandlerRegistry(IServiceCollection services)
        {
            for (var i = 0; i < services.Count; i++) {
                var descriptor = services[i];
                if (descriptor.ServiceType == typeof(ICommandHandlerRegistry)) {
                    if (i > 16) {
                        // Let's move it to the beginning of the list
                        // to speed up future searches
                        services.RemoveAt(i);
                        services.Insert(0, descriptor);
                    }
                    return (ICommandHandlerRegistry?) descriptor.ImplementationInstance
                        ?? throw Errors.CommandHandlerRegistryMustBeRegisteredAsInstance();
                }
            }
            return null;
        }

        // Handler discovery

        public CommanderBuilder AddHandlers<TService>(double? priorityOverride = null)
            => AddHandlers(typeof(TService), priorityOverride);
        public CommanderBuilder AddHandlers<TService, TImplementation>(double? priorityOverride = null)
            => AddHandlers(typeof(TService), typeof(TImplementation), priorityOverride);
        public CommanderBuilder AddHandlers(Type serviceType, double? priorityOverride = null)
            => AddHandlers(serviceType, serviceType, priorityOverride);
        public CommanderBuilder AddHandlers(Type serviceType, Type implementationType, double? priorityOverride = null)
        {
            if (!serviceType.IsAssignableFrom(implementationType))
                throw new ArgumentOutOfRangeException(nameof(implementationType));

            var interfaceMethods = new HashSet<MethodInfo>();

            // Interfaces
            var tInterfaces = implementationType.GetInterfaces();
            foreach (var tInterface in tInterfaces) {
                if (!tInterface.IsGenericType)
                    continue;
                var gInterface = tInterface.GetGenericTypeDefinition();
                if (gInterface != typeof(ICommandHandler<>))
                    continue;
                var tCommand = tInterface.GetGenericArguments().SingleOrDefault();
                if (tCommand == null)
                    continue;

                var method = implementationType.GetInterfaceMap(tInterface).TargetMethods.Single();
                var attr = MethodCommandHandler.GetAttribute(method);
                var isEnabled = attr?.IsEnabled ?? true;
                if (!isEnabled)
                    continue;
                var isFilter = attr?.IsFilter ?? false;
                var order = priorityOverride ?? attr?.Priority ?? 0;
                AddHandler(InterfaceCommandHandler.New(serviceType, tCommand, isFilter, order));
                interfaceMethods.Add(method);
            }

            // Methods
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static
                | BindingFlags.FlattenHierarchy;
            var methods = implementationType.GetMethods(bindingFlags);
            if (implementationType.IsInterface) {
                var tOtherInterfaces = tInterfaces
                    .Where(t => !typeof(ICommandHandler).IsAssignableFrom(t))
                    .ToList();
                var lMethods = new List<MethodInfo>(methods);
                foreach (var tOtherInterface in tOtherInterfaces)
                    lMethods.AddRange(tOtherInterface.GetMethods(bindingFlags));
                methods = lMethods.ToArray();
            }
            foreach (var method in methods) {
                if (interfaceMethods.Contains(method))
                    continue;
                var handler = MethodCommandHandler.TryNew(serviceType, method, priorityOverride);
                if (handler == null)
                    continue;
                AddHandler(handler);
            }

            return this;
        }

        // AddCommandService

        public CommanderBuilder AddCommandService<TService>(
            ServiceLifetime lifetime = ServiceLifetime.Singleton,
            double? priorityOverride = null)
            where TService : class
            => AddCommandService(typeof(TService), lifetime, priorityOverride);
        public CommanderBuilder AddCommandService<TService, TImplementation>(
            ServiceLifetime lifetime = ServiceLifetime.Singleton,
            double? priorityOverride = null)
            where TService : class
            where TImplementation : class, TService
            => AddCommandService(typeof(TService), typeof(TImplementation), lifetime, priorityOverride);

        public CommanderBuilder AddCommandService(
            Type serviceType,
            ServiceLifetime lifetime = ServiceLifetime.Singleton,
            double? priorityOverride = null)
            => AddCommandService(serviceType, serviceType, lifetime, priorityOverride);
        public CommanderBuilder AddCommandService(
            Type serviceType, Type implementationType,
            ServiceLifetime lifetime = ServiceLifetime.Singleton,
            double? priorityOverride = null)
        {
            if (!serviceType.IsAssignableFrom(implementationType))
                throw new ArgumentOutOfRangeException(nameof(implementationType));

            object Factory(IServiceProvider c)
            {
                // We should try to validate it here because if the type doesn't
                // have any virtual methods (which might be a mistake), no calls
                // will be intercepted, so no error will be thrown later.
                var interceptor = c.GetRequiredService<CommandServiceInterceptor>();
                interceptor.ValidateType(implementationType);
                var proxyGenerator = c.GetRequiredService<ICommandServiceProxyGenerator>();
                var proxyType = proxyGenerator.GetProxyType(implementationType);
                return c.Activate(proxyType);
            }

            var descriptor = new ServiceDescriptor(serviceType, Factory, lifetime);
            Services.TryAdd(descriptor);
            AddHandlers(serviceType, implementationType, priorityOverride);
            return this;
        }

        // Low-level methods

        public CommanderBuilder AddHandler<TService, TCommand>(double priority = 0)
            where TService : class
            where TCommand : class, ICommand
            => AddHandler<TService, TCommand>(false, priority);

        public CommanderBuilder AddHandler<TService, TCommand>(bool isFilter, double priority = 0)
            where TService : class
            where TCommand : class, ICommand
            => AddHandler(InterfaceCommandHandler.New<TService, TCommand>(isFilter, priority));

        public CommanderBuilder AddHandler(Type serviceType, MethodInfo methodInfo, double? priorityOverride = null)
            => AddHandler(MethodCommandHandler.New(serviceType, methodInfo, priorityOverride));

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
