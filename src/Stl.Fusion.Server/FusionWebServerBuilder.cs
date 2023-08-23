using Stl.Fusion.Server.Authentication;
using Stl.Fusion.Server.Endpoints;
using Stl.Fusion.Server.Internal;
using Stl.Fusion.Server.Middlewares;
using Stl.Fusion.Server.Rpc;
using Stl.Rpc;
using Stl.Rpc.Server;

namespace Stl.Fusion.Server;

[StructLayout(LayoutKind.Auto)]
public readonly struct FusionWebServerBuilder
{
    private class AddedTag;
    private class ControllersAddedTag;
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());
    private static readonly ServiceDescriptor ControllersAddedTagDescriptor = new(typeof(ControllersAddedTag), new ControllersAddedTag());

    public FusionBuilder Fusion { get; }
    public IServiceCollection Services => Fusion.Services;

    internal FusionWebServerBuilder(
        FusionBuilder fusion,
        Action<FusionWebServerBuilder>? configure)
    {
        Fusion = fusion;
        var services = Services;
        if (services.Contains(AddedTagDescriptor)) {
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        services.Insert(0, AddedTagDescriptor);

        // Add Rpc-related services
        var rpc = fusion.Rpc;
        rpc.AddWebSocketServer();
        rpc.AddInboundMiddleware<DefaultSessionReplacerRpcMiddleware>();
        services.AddSingleton(_ => new SessionBoundRpcConnectionFactory());
        services.AddSingleton<RpcServerConnectionFactory>(
            c => c.GetRequiredService<SessionBoundRpcConnectionFactory>().Invoke);

        // Add other services
        services.AddSingleton(_ => SessionMiddleware.Options.Default);
        services.AddScoped(c => new SessionMiddleware(c.GetRequiredService<SessionMiddleware.Options>(), c));
        services.AddSingleton(_ => ServerAuthHelper.Options.Default);
        services.AddScoped(c => new ServerAuthHelper(c.GetRequiredService<ServerAuthHelper.Options>(), c));
        services.AddSingleton(_ => new AuthSchemasCache());
        services.AddSingleton(_ => AuthEndpoints.Options.Default);
        services.AddSingleton(c => new AuthEndpoints(c.GetRequiredService<AuthEndpoints.Options>()));
        services.AddSingleton(_ => new BlazorModeEndpoint());

        configure?.Invoke(this);
    }

    public FusionMvcWebServerBuilder AddMvc()
        => new(this, null);

    public FusionWebServerBuilder AddMvc(Action<FusionMvcWebServerBuilder> configure)
        => new FusionMvcWebServerBuilder(this, configure).FusionWebServer;

    public FusionWebServerBuilder ConfigureSessionMiddleware(
        Func<IServiceProvider, SessionMiddleware.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    public FusionWebServerBuilder ConfigureAuthEndpoint(
        Func<IServiceProvider, AuthEndpoints.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }

    public FusionWebServerBuilder ConfigureServerAuthHelper(
        Func<IServiceProvider, ServerAuthHelper.Options> optionsFactory)
    {
        Services.AddSingleton(optionsFactory);
        return this;
    }
}
