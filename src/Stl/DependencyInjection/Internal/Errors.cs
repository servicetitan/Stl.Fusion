using System;

namespace Stl.DependencyInjection.Internal
{
    public static class Errors
    {
        public static Exception NoService(ServiceRef serviceRef)
            => new InvalidOperationException($"No service for {serviceRef}.");

        public static Exception NoServiceRef(Type serviceType)
            => new InvalidOperationException($"Can't find a way to create ServiceRef for '{serviceType}'.");
    }
}
