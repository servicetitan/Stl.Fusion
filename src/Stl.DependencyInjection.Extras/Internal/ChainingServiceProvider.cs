namespace Stl.DependencyInjection.Extras.Internal;

public sealed class ChainingServiceProvider : IServiceProvider
{
    public IServiceProvider PrimaryServiceProvider { get; }
    public IServiceProvider SecondaryServiceProvider { get; }

    public ChainingServiceProvider(
        IServiceProvider primaryServiceProvider,
        IServiceProvider secondaryServiceProvider)
    {
        PrimaryServiceProvider = primaryServiceProvider;
        SecondaryServiceProvider = secondaryServiceProvider;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IServiceProvider))
            return this;
        return PrimaryServiceProvider.GetService(serviceType) ?? SecondaryServiceProvider.GetService(serviceType);
    }
}
