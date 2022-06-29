namespace Stl.Fusion.Client;

public static class FusionBuilderExt
{
    public static FusionRestEaseClientBuilder AddRestEaseClient(this FusionBuilder fusion,
        Func<IServiceProvider, WebSocketChannelProvider.Options>? optionsFactory = null)
    {
        var builder = new FusionRestEaseClientBuilder(fusion);
        if (optionsFactory != null)
            builder.ConfigureWebSocketChannel(optionsFactory);
        return builder;
    }

    public static FusionBuilder AddRestEaseClient(this FusionBuilder fusion,
        Action<FusionRestEaseClientBuilder> configureClient,
        Func<IServiceProvider, WebSocketChannelProvider.Options>? optionsFactory = null)
    {
        var restEaseClient = fusion.AddRestEaseClient(optionsFactory);
        configureClient(restEaseClient);
        return fusion;
    }

}
