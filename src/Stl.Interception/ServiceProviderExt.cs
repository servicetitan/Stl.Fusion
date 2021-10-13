using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Interception
{
    public static class ServiceProviderExt
    {
        // GetTypeViewFactory

        public static ITypeViewFactory TypeViewFactory(this IServiceProvider services)
            => services.GetService<ITypeViewFactory>() ?? Interception.TypeViewFactory.Default;

        public static TypeViewFactory<TView> TypeViewFactory<TView>(this IServiceProvider services)
            where TView : class
            => services.TypeViewFactory().For<TView>();
    }
}
