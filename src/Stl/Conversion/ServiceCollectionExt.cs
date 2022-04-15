using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Conversion.Internal;

namespace Stl.Conversion;

public static class ServiceCollectionExt
{
    public static IServiceCollection AddConverters(
        this IServiceCollection services,
        Type? sourceConverterProviderGenericType = null)
    {
        sourceConverterProviderGenericType ??= typeof(DefaultSourceConverterProvider<>);
        services.TryAddSingleton<IConverterProvider, DefaultConverterProvider>();
        services.TryAddSingleton(typeof(ISourceConverterProvider<>), sourceConverterProviderGenericType);
        return services;
    }
}
