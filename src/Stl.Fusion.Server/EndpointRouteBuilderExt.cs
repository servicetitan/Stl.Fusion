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

    public static IEndpointRouteBuilder MapFusionBlazorSwitch(this IEndpointRouteBuilder endpoints)
    {
        var services = endpoints.ServiceProvider;
        var handler = services.GetRequiredService<BlazorSwitchEndpoint>();
        endpoints
            .MapGet("/fusion/blazorMode/{isServerSideBlazor}", handler.Invoke)
            .WithGroupName("FusionBlazorSwitch");
        return endpoints;
    }
}

#endif
