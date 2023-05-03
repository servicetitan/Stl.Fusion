using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Rpc.Infrastructure;
using Stl.Rpc.Internal;

namespace Stl.Rpc;

public readonly struct RpcBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

    public IServiceCollection Services { get; }
    public RpcConfiguration Configuration { get; }

    internal RpcBuilder(
        IServiceCollection services,
        Action<RpcBuilder>? configure)
    {
        Services = services;
        if (Services.Contains(AddedTagDescriptor)) {
            // Already configured
            Configuration = GetOptions(services);
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        Services.Insert(0, AddedTagDescriptor);

        // Common services
        Services.TryAddSingleton(new RpcConfiguration());
        Services.TryAddSingleton(c => new RpcChannelHub(c));
        Services.TryAddSingleton<Func<Symbol, RpcChannel>>(c => name => new RpcChannel(name, c));

        // Infrastructure
        Services.TryAddSingleton(c => new RpcRequestBinder(c));
        Services.TryAddSingleton(c => new RpcRequestHandler(c));
        Services.TryAddSingleton(c => new RpcServiceRegistry(c));
        Services.TryAddSingleton(c => new RpcSystemCallService(c));

        Configuration = GetOptions(services);
        AddService<RpcSystemCallService>("$sys");
    }

    public RpcBuilder AddService<TService>(Symbol serviceName = default)
        => AddService(typeof(TService), serviceName);
    public RpcBuilder AddService<TService, TImplementation>(Symbol serviceName = default)
        where TImplementation : class, TService
        => AddService(typeof(TService), typeof(TImplementation), serviceName);
    public RpcBuilder AddService(Type serviceType, Symbol serviceName = default)
        => AddService(serviceType, null, serviceName);
    public RpcBuilder AddService(Type serviceType, Type? implementationType, Symbol serviceName = default)
    {
        if (serviceType.IsValueType)
            throw new ArgumentOutOfRangeException(nameof(serviceType));
        if (Configuration.Services.ContainsKey(serviceType))
            throw Stl.Internal.Errors.KeyAlreadyExists();

        if (serviceName.IsEmpty)
            serviceName = Configuration.ServiceNameBuilder.Invoke(serviceType);
        if (implementationType != null) {
            if (!serviceType.IsAssignableFrom(implementationType))
                throw new ArgumentOutOfRangeException(nameof(implementationType));

            Configuration.Implementations.Add(serviceType, implementationType);
        }
        Configuration.Services.Add(serviceType, serviceName);
        return this;
    }

    public RpcBuilder RemoveService<TService>()
        => RemoveService(typeof(TService));
    public RpcBuilder RemoveService(Type serviceType)
    {
        Configuration.Services.Remove(serviceType);
        Configuration.Implementations.Remove(serviceType);
        return this;
    }

    public RpcBuilder ClearServices()
    {
        Configuration.Services.Clear();
        Configuration.Implementations.Clear();
        return this;
    }

    // Private methods

    private static RpcConfiguration GetOptions(IServiceCollection services)
    {
        for (var i = 0; i < services.Count; i++) {
            var descriptor = services[i];
            if (descriptor.ServiceType == typeof(RpcConfiguration)) {
                if (i > 16) {
                    // Let's move it to the beginning of the list
                    // to speed up future searches
                    services.RemoveAt(i);
                    services.Insert(0, descriptor);
                }
                return (RpcConfiguration?) descriptor.ImplementationInstance
                    ?? throw Errors.RpcOptionsMustBeRegisteredAsInstance();
            }
        }
        throw Errors.RpcOptionsIsNotRegistered();
    }
}
