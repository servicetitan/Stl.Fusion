using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.CommandR.Diagnostics;
using Stl.CommandR.Interception;
using Stl.CommandR.Internal;
using Stl.Interception;

namespace Stl.CommandR;

public readonly struct CommanderBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

    public IServiceCollection Services { get; }
    public HashSet<CommandHandler> Handlers { get; }

    internal CommanderBuilder(
        IServiceCollection services,
        Action<CommanderBuilder>? configure)
    {
        Services = services;
        if (services.Contains(AddedTagDescriptor)) {
            // Already configured
            Handlers = GetCommandHandlers(services);
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        services.Insert(0, AddedTagDescriptor);

        // Common services
        services.TryAddSingleton<ICommander>(c => new Commander(c));
        services.TryAddSingleton(new HashSet<CommandHandler>());
        services.TryAddSingleton(c => new CommandHandlerRegistry(c));
        services.TryAddSingleton(c => new CommandHandlerResolver(c));

        // Command services & their dependencies
        Services.TryAddSingleton(_ => new CommandServiceInterceptor.Options());
        Services.TryAddSingleton(c => new CommandServiceInterceptor(
            c.GetRequiredService<CommandServiceInterceptor.Options>(), c));
        Handlers = GetCommandHandlers(services);

        // Default handlers
        services.AddSingleton(_ => new PreparedCommandHandler());
        AddHandlers<PreparedCommandHandler>();
        services.AddSingleton(c => new CommandTracer(c));
        AddHandlers<CommandTracer>();
        services.AddSingleton(_ => new LocalCommandRunner());
        AddHandlers<LocalCommandRunner>();

        configure?.Invoke(this);
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
            throw Stl.Internal.Errors.MustBeAssignableTo(implementationType, serviceType, nameof(implementationType));

        var interfaceMethods = new HashSet<MethodInfo>();

        // ICommandHandler<TCommand> interfaces
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
            if (!method.ReturnType.IsTaskOrValueTask())
                continue;
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                continue;
            if (!typeof(ICommand).IsAssignableFrom(parameters[0].ParameterType))
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
        where TService : class, ICommandService
        => AddCommandService(typeof(TService), lifetime, priorityOverride);
    public CommanderBuilder AddCommandService<TService, TImplementation>(
        ServiceLifetime lifetime = ServiceLifetime.Singleton,
        double? priorityOverride = null)
        where TService : class
        where TImplementation : class, TService, ICommandService
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
            throw Stl.Internal.Errors.MustBeAssignableTo(implementationType, serviceType, nameof(implementationType));
        if (!typeof(ICommandService).IsAssignableFrom(implementationType))
            throw Stl.Internal.Errors.MustImplement<ICommandService>(implementationType, nameof(implementationType));

        var descriptor = new ServiceDescriptor(serviceType, c => CommanderProxies.NewServiceProxy(c, implementationType), lifetime);
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

    public CommanderBuilder AddHandler(Type serviceType, MethodInfo method, double? priorityOverride = null)
        => AddHandler(MethodCommandHandler.New(serviceType, method, priorityOverride));

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

    // Filters

    public CommanderBuilder AddHandlerFilter(CommandHandlerFilter commandHandlerFilter)
    {
        Services.AddSingleton(commandHandlerFilter);
        return this;
    }

    public CommanderBuilder AddHandlerFilter<TCommandHandlerFilter>()
        where TCommandHandlerFilter : CommandHandlerFilter
    {
        Services.AddSingleton<CommandHandlerFilter, TCommandHandlerFilter>();
        return this;
    }

    public CommanderBuilder AddHandlerFilter<TCommandHandlerFilter>(
        Func<IServiceProvider, TCommandHandlerFilter> factory)
        where TCommandHandlerFilter : CommandHandlerFilter
    {
        Services.AddSingleton<CommandHandlerFilter, TCommandHandlerFilter>(factory);
        return this;
    }

    public CommanderBuilder AddHandlerFilter(Func<CommandHandler, Type, bool> commandHandlerFilter)
        => AddHandlerFilter(c => new FuncCommandHandlerFilter(commandHandlerFilter));

    public CommanderBuilder AddHandlerFilter(
        Func<IServiceProvider, Func<CommandHandler, Type, bool>> commandHandlerFilterFactory)
        => AddHandlerFilter(c => {
            var filter = commandHandlerFilterFactory(c);
            return new FuncCommandHandlerFilter(filter);
        });

    public CommanderBuilder ClearHandlerFilters()
    {
        Services.RemoveAll<CommandHandlerFilter>();
        return this;
    }

    // Private methods

    private static HashSet<CommandHandler> GetCommandHandlers(IServiceCollection services)
    {
        for (var i = 0; i < services.Count; i++) {
            var descriptor = services[i];
            if (descriptor.ServiceType == typeof(HashSet<CommandHandler>)) {
                if (i > 16) {
                    // Let's move it to the beginning of the list
                    // to speed up future searches
                    services.RemoveAt(i);
                    services.Insert(0, descriptor);
                }
                return (HashSet<CommandHandler>?) descriptor.ImplementationInstance
                    ?? throw Errors.CommandHandlerSetMustBeRegisteredAsInstance();
            }
        }
        throw Errors.CommandHandlerSetIsNotRegistered();
    }
}
