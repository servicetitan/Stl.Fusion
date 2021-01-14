using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Interception
{
    public static class ServiceProviderEx
    {
        // GetTypeViewFactory

        public static ITypeViewFactory GetTypeViewFactory(this IServiceProvider services)
            => services.GetService<ITypeViewFactory>() ?? TypeViewFactory.Default;

        public static TypeViewFactory<TView> GetTypeViewFactory<TView>(this IServiceProvider services)
            where TView : class
            => services.GetTypeViewFactory().For<TView>();
    }
}
