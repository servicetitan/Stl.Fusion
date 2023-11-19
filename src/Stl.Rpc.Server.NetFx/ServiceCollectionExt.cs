using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl.Rpc.Server;

public static class ServiceCollectionExt
{
    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public static IServiceCollection AddControllersAsServices(this IServiceCollection services, IEnumerable<Type> controllerTypes)
    {
        foreach (var type in controllerTypes)
            services.AddTransient(type);
        return services;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public static IServiceCollection AddControllersAsServices(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
            services.AddControllersAsServices(assembly);
        return services;
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public static IServiceCollection AddControllersAsServices(this IServiceCollection services, Assembly assembly)
    {
        services.AddControllersAsServices(assembly.GetControllerTypes());
        return services;
    }
}
