using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Stl.Fusion.Server.Endpoints;

namespace Stl.Fusion.Server;

#if NET7_0_OR_GREATER

public static class EndpointRouteBuilderExt
{
    public static IEndpointRouteBuilder MapFusionAuth(this IEndpointRouteBuilder endpoints)
    {
        var services = endpoints.ServiceProvider;
        var handler = services.GetRequiredService<AuthEndpoints>();
        endpoints
            .MapGet("/signIn", handler.SignIn)
            .WithGroupName("FusionAuth");
        endpoints
            .MapGet("/signIn/{scheme}", handler.SignIn)
            .WithGroupName("FusionAuth");
        endpoints
            .MapGet("/signOut", handler.SignOut)
            .WithGroupName("FusionAuth");
        return endpoints;
    }

    public static IEndpointRouteBuilder MapFusionBlazorMode(this IEndpointRouteBuilder endpoints)
    {
        var services = endpoints.ServiceProvider;
        var handler = services.GetRequiredService<BlazorModeEndpoint>();
        endpoints
            .MapGet("/fusion/blazorMode", handler.Invoke)
            .WithGroupName("FusionBlazorMode");
        endpoints
            .MapGet("/fusion/blazorMode/{isBlazorServer}", handler.Invoke)
            .WithGroupName("FusionBlazorMode");
        return endpoints;
    }
}

#endif
