namespace Stl.DependencyInjection.Extras.Internal;

public sealed class FuncServiceProvider : IServiceProvider
{
    public Func<Type, object?> ServiceProvider { get; }

    public FuncServiceProvider(Func<Type, object?> serviceProvider)
        => ServiceProvider = serviceProvider;

    public static implicit operator FuncServiceProvider(Func<Type, object?> serviceProvider)
        => new(serviceProvider);

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
            return this;
        return ServiceProvider.Invoke(serviceType);
    }
}
