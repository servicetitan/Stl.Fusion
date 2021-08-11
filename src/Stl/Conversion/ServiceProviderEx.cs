using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Conversion
{
    public static class ServiceProviderEx
    {
        public static IConverterProvider Converters(this IServiceProvider services)
            => services.GetService<IConverterProvider>() ?? ConverterProvider.Default;
    }
}
