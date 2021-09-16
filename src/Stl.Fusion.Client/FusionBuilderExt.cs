using System;

namespace Stl.Fusion.Client
{
    public static class FusionBuilderExt
    {
        public static FusionRestEaseClientBuilder AddRestEaseClient(this FusionBuilder fusion,
            Action<IServiceProvider, WebSocketChannelProvider.Options>? optionsBuilder = null)
        {
            var builder = new FusionRestEaseClientBuilder(fusion);
            if (optionsBuilder != null)
                builder.ConfigureWebSocketChannel(optionsBuilder);
            return builder;
        }

        public static FusionBuilder AddRestEaseClient(this FusionBuilder fusion,
            Action<FusionRestEaseClientBuilder> configureClient,
            Action<IServiceProvider, WebSocketChannelProvider.Options>? optionsBuilder = null)
        {
            var restEaseClient = fusion.AddRestEaseClient(optionsBuilder);
            configureClient.Invoke(restEaseClient);
            return fusion;
        }

    }
}
