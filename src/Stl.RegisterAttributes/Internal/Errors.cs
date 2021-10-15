using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Stl.RegisterAttributes.Internal;

public static class Errors
{
    public static Exception NoServiceAttribute(Type implementationType)
        => new InvalidOperationException(
            $"No matching [{nameof(RegisterAttribute)}] descendant is found " +
            $"on '{implementationType}'.");

    public static Exception HostedServiceHasToBeSingleton(Type implementationType)
        => new InvalidOperationException(
            $"'{implementationType}' has to use {nameof(RegisterServiceAttribute.Lifetime)} == " +
            $"{nameof(ServiceLifetime)}.{nameof(ServiceLifetime.Singleton)} " +
            $"to be registered as {nameof(IHostedService)}.");
}
