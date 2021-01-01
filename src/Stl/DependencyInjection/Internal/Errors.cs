using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Stl.DependencyInjection.Internal
{
    public static class Errors
    {
        public static Exception NoService(ServiceRef serviceRef)
            => new InvalidOperationException($"No service for {serviceRef}.");

        public static Exception NoServiceAttribute(Type implementationType) =>
            new InvalidOperationException(
                $"No matching [{nameof(ServiceAttributeBase)}] descendant is found " +
                $"on '{implementationType}'.");

        public static Exception HostedServiceHasToBeSingleton(Type implementationType) =>
            new InvalidOperationException(
                $"'{implementationType}' has to use {nameof(ServiceAttribute.Lifetime)} == " +
                $"{nameof(ServiceLifetime)}.{nameof(ServiceLifetime.Singleton)} " +
                $"to be registered as {nameof(IHostedService)}.");
    }
}
