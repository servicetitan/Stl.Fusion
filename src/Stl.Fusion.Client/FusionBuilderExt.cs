using Microsoft.Extensions.Http;

namespace Stl.Fusion.Client;

public static class FusionBuilderExt
{
    public static FusionRestEaseClientBuilder AddRestEaseClient(
        this FusionBuilder fusion,
        Func<IServiceProvider, WebSocketChannelProvider.Options>? webSocketChannelProviderOptionsFactory = null,
        Action<IServiceProvider, string?, HttpClientFactoryOptions>? httpClientFactoryOptionsBuilder = null)
    {
        var builder = new FusionRestEaseClientBuilder(fusion);
        if (webSocketChannelProviderOptionsFactory != null)
            builder.ConfigureWebSocketChannel(webSocketChannelProviderOptionsFactory);
        if (httpClientFactoryOptionsBuilder != null)
            builder.ConfigureHttpClient(httpClientFactoryOptionsBuilder);
        return builder;
    }

    public static FusionBuilder AddRestEaseClient(
        this FusionBuilder fusion,
        Action<FusionRestEaseClientBuilder> configureClient)
    {
        var restEaseClient = fusion.AddRestEaseClient();
        configureClient(restEaseClient);
        return fusion;
    }

}
