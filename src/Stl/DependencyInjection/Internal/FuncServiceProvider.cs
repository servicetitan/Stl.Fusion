using System;

namespace Stl.DependencyInjection.Internal
{
    public sealed class FuncServiceProvider : IServiceProvider
    {
        public Func<Type, object?> ServiceProvider { get; }

        public FuncServiceProvider(Func<Type, object?> serviceProvider)
            => ServiceProvider = serviceProvider;

        public static implicit operator FuncServiceProvider(Func<Type, object?> serviceProvider)
            => new FuncServiceProvider(serviceProvider);

        public object? GetService(Type serviceType)
            => ServiceProvider.Invoke(serviceType);
    }
}
