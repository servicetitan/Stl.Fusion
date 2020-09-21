using System;

namespace Stl.Fusion.Client
{
    public static class FusionBuilderEx
    {
        public static FusionRestEaseClientBuilder AddRestEaseClient(this FusionBuilder fusion,
            Action<IServiceProvider, WebSocketChannelProvider.Options>? optionsBuilder = null)
        {
            var builder = new FusionRestEaseClientBuilder(fusion);
            if (optionsBuilder != null)
                builder.ConfigureWebSocketChannel(optionsBuilder);
            return builder;
        }
    }
}
