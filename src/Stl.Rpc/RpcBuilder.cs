using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public readonly struct RpcBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

    public IServiceCollection Services { get; }
    public RpcOptions Options { get; }

    internal RpcBuilder(
        IServiceCollection services,
        Action<RpcBuilder>? configure)
    {
        Services = services;
        if (Services.Contains(AddedTagDescriptor)) {
            // Already configured
            Options = GetOptions(services);
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        Services.Insert(0, AddedTagDescriptor);

        // Common services
        Services.TryAddSingleton(new RpcOptions());
        Services.TryAddSingleton(RpcChannelOptions.DefaultOptionsProvider);
        Services.TryAddTransient(c => new RpcChannel(c));

        // Infrastructure
        Services.TryAddSingleton(c => new RpcMiddlewareRegistry(c));
        Services.TryAddSingleton(c => new RpcServiceRegistry(c));
        Services.TryAddSingleton(_ => new RpcMethodResolver());

        Options = GetOptions(services);
    }

    public RpcBuilder AddService<TService>()
        => AddService(typeof(TService));
    public RpcBuilder AddService<TService>(Symbol serviceName)
        => AddService(serviceName, typeof(TService));
    public RpcBuilder AddService(Type serviceType)
        => AddService(serviceType.GetName(), serviceType);
    public RpcBuilder AddService(Symbol serviceName, Type serviceType)
    {
        if (serviceName.IsEmpty)
            throw new ArgumentOutOfRangeException(nameof(serviceName));
        if (serviceType.IsValueType)
            throw new ArgumentOutOfRangeException(nameof(serviceType));

        if (Options.ServiceNames.ContainsKey(serviceName))
            throw Stl.Internal.Errors.KeyAlreadyExists();
        if (!Options.ServiceTypes.ContainsKey(serviceType))
            throw Stl.Internal.Errors.KeyAlreadyExists();

        Options.ServiceNames.Add(serviceName, serviceType);
        Options.ServiceTypes.Add(serviceType, serviceName);
        return this;
    }

    public RpcBuilder RemoveService(Symbol serviceName)
    {
        var serviceType = Options.ServiceNames.GetValueOrDefault(serviceName);
        if (serviceType == null)
            throw new KeyNotFoundException();

        Options.ServiceNames.Remove(serviceName);
        Options.ServiceTypes.Remove(serviceType);
        return this;
    }

    public RpcBuilder RemoveService<TService>()
        => RemoveService(typeof(TService));
    public RpcBuilder RemoveService(Type serviceType)
    {
        var serviceName = Options.ServiceTypes.GetValueOrDefault(serviceType);
        if (serviceName.IsEmpty)
            throw new KeyNotFoundException();

        Options.ServiceNames.Remove(serviceName);
        Options.ServiceTypes.Remove(serviceType);
        return this;
    }

    public RpcBuilder ClearServices()
    {
        Options.ServiceNames.Clear();
        Options.ServiceTypes.Clear();
        return this;
    }

    public RpcBuilder AddMiddleware<TMiddleware>()
        where TMiddleware : RpcMiddleware
        => AddMiddleware(typeof(TMiddleware));
    public RpcBuilder AddMiddleware(Type middlewareType)
    {
        if (!typeof(RpcMiddleware).IsAssignableFrom(middlewareType))
            throw new ArgumentOutOfRangeException(nameof(middlewareType));

        Options.MiddlewareTypes.Add(middlewareType);
        return this;
    }

    public RpcBuilder RemoveMiddleware<TMiddleware>()
        where TMiddleware : RpcMiddleware
        => RemoveMiddleware(typeof(TMiddleware));
    public RpcBuilder RemoveMiddleware(Type middlewareType)
    {
        Options.MiddlewareTypes.Remove(middlewareType);
        return this;
    }

    public RpcBuilder ClearMiddlewares()
    {
        Options.MiddlewareTypes.Clear();
        return this;
    }

    // Private methods

    private static RpcOptions GetOptions(IServiceCollection services)
    {
        for (var i = 0; i < services.Count; i++) {
            var descriptor = services[i];
            if (descriptor.ServiceType == typeof(RpcOptions)) {
                if (i > 16) {
                    // Let's move it to the beginning of the list
                    // to speed up future searches
                    services.RemoveAt(i);
                    services.Insert(0, descriptor);
                }
                return (RpcOptions?) descriptor.ImplementationInstance
                    ?? throw Errors.RpcOptionsMustBeRegisteredAsInstance();
            }
        }
        throw Errors.RpcOptionsIsNotRegistered();
    }
}
