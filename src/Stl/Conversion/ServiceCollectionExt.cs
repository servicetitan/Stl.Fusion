using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Conversion.Internal;

namespace Stl.Conversion;

public static class ServiceCollectionExt
{
    public static IServiceCollection AddConverters(
        this IServiceCollection services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type? sourceConverterProviderGenericType = null)
    {
        sourceConverterProviderGenericType ??= typeof(DefaultSourceConverterProvider<>);
        services.TryAddSingleton<IConverterProvider, DefaultConverterProvider>();
        services.TryAddSingleton(typeof(ISourceConverterProvider<>), sourceConverterProviderGenericType);
        return services;
    }
}
