using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public readonly struct RpcBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

    public IServiceCollection Services { get; }
    public RpcGlobalOptions GlobalOptions { get; }

    internal RpcBuilder(
        IServiceCollection services,
        Action<RpcBuilder>? configure)
    {
        Services = services;
        if (Services.Contains(AddedTagDescriptor)) {
            // Already configured
            GlobalOptions = GetOptions(services);
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        Services.Insert(0, AddedTagDescriptor);

        // Common services
        Services.TryAddSingleton(new RpcGlobalOptions());
        Services.TryAddSingleton(RpcChannelOptions.DefaultOptionsProvider);
        Services.TryAddTransient(c => new RpcChannel(c));

        // Infrastructure
        Services.TryAddSingleton(c => new RpcRequestBinder(c));
        Services.TryAddSingleton(c => new RpcRequestHandler(c));
        Services.TryAddSingleton(c => new RpcServiceRegistry(c));
        Services.TryAddSingleton(c => new RpcSystemCallService(c));

        GlobalOptions = GetOptions(services);
        AddService<RpcSystemCallService>("$sys");
    }

    public RpcBuilder AddService<TService>(Symbol serviceName = default)
        => AddService(typeof(TService), serviceName);
    public RpcBuilder AddService(Type serviceType, Symbol serviceName = default)
    {
        if (serviceType.IsValueType)
            throw new ArgumentOutOfRangeException(nameof(serviceType));
        if (GlobalOptions.ServiceTypes.ContainsKey(serviceType))
            throw Stl.Internal.Errors.KeyAlreadyExists();

        if (serviceName.IsEmpty)
            serviceName = GlobalOptions.ServiceNameBuilder.Invoke(serviceType);
        GlobalOptions.ServiceTypes.Add(serviceType, (serviceName, null));
        return this;
    }

    public RpcBuilder RemoveService<TService>()
        => RemoveService(typeof(TService));
    public RpcBuilder RemoveService(Type serviceType)
    {
        GlobalOptions.ServiceTypes.Remove(serviceType);
        return this;
    }

    public RpcBuilder AddServiceImplementation<TService, TImplementation>()
        => AddServiceImplementation(typeof(TService), typeof(TImplementation));
    public RpcBuilder AddServiceImplementation(Type serviceType, Type implementationType)
    {
        if (serviceType.IsValueType)
            throw new ArgumentOutOfRangeException(nameof(serviceType));
        if (GlobalOptions.ServiceTypes.ContainsKey(implementationType))
            throw Stl.Internal.Errors.KeyAlreadyExists();

        GlobalOptions.ServiceTypes.Add(implementationType, (Symbol.Empty, serviceType));
        return this;
    }

    public RpcBuilder RemoveServiceImplementation<TImplementation>()
        => RemoveService(typeof(TImplementation));
    public RpcBuilder RemoveServiceImplementation(Type implementationType)
    {
        GlobalOptions.ServiceTypes.Remove(implementationType);
        return this;
    }

    public RpcBuilder ClearServices()
    {
        GlobalOptions.ServiceTypes.Clear();
        return this;
    }

    // Private methods

    private static RpcGlobalOptions GetOptions(IServiceCollection services)
    {
        for (var i = 0; i < services.Count; i++) {
            var descriptor = services[i];
            if (descriptor.ServiceType == typeof(RpcGlobalOptions)) {
                if (i > 16) {
                    // Let's move it to the beginning of the list
                    // to speed up future searches
                    services.RemoveAt(i);
                    services.Insert(0, descriptor);
                }
                return (RpcGlobalOptions?) descriptor.ImplementationInstance
                    ?? throw Errors.RpcOptionsMustBeRegisteredAsInstance();
            }
        }
        throw Errors.RpcOptionsIsNotRegistered();
    }
}
