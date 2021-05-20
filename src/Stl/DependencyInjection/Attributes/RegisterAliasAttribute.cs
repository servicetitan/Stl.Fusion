using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.DependencyInjection
{
    public class RegisterAliasAttribute : RegisterAttribute
    {
        private static readonly MethodInfo ServiceFactoryMethod =
            typeof(RegisterAliasAttribute).GetMethod(
                nameof(ServiceFactory), BindingFlags.Static | BindingFlags.NonPublic)!;

        public Type ServiceType { get; set; }
        public Type? ActualServiceType { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
        public bool IsEnumerable { get; set; }

        public RegisterAliasAttribute(Type serviceType, Type? actualServiceType = null)
        {
            ServiceType = serviceType;
            ActualServiceType = actualServiceType;
        }

        public override void Register(IServiceCollection services, Type implementationType)
        {
            var actualServiceType = ActualServiceType ?? implementationType;
            var delegateType = typeof(Func<,>).MakeGenericType(
                typeof(IServiceProvider), actualServiceType);
            var factory = (Func<IServiceProvider, object>)
                Delegate.CreateDelegate(delegateType,
                    ServiceFactoryMethod.MakeGenericMethod(actualServiceType));

            var descriptor = new ServiceDescriptor(ServiceType, factory, Lifetime);
            if (IsEnumerable)
                services.TryAddEnumerable(descriptor);
            else
                services.TryAdd(descriptor);
        }

        private static TService ServiceFactory<TService>(IServiceProvider serviceProvider)
            where TService : class
            => serviceProvider.GetRequiredService<TService>();
    }
}
