using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Rpc.Server;

public readonly struct FusionWebServerBuilder
{
    public FusionBuilder Fusion { get; }
    public IServiceCollection Services => Fusion.Services;

    internal FusionWebServerBuilder(
        FusionBuilder fusion,
        Action<FusionWebServerBuilder>? configure)
    {
        Fusion = fusion;
        var services = Services;
        if (services.HasService<WebSocketServer>()) {
            configure?.Invoke(this);
            return;
        }

        Fusion.AddPublisher();
        services.TryAddSingleton<WebSocketServer.Options>();
        services.TryAddSingleton<WebSocketServer>();

        // TODO: configure model binder providers

        configure?.Invoke(this);
    }

    public FusionWebServerBuilder ConfigureWebSocketServer(Func<IServiceProvider, WebSocketServer.Options> webSocketServerOptionsFactory)
    {
        Services.AddSingleton(webSocketServerOptionsFactory);
        return this;
    }

    // TODO: add AddControllers and AddControllerFilter
}
