using System;
using Microsoft.Extensions.Http;

namespace Stl.Fusion.Client.RestEase.Internal
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class FusionHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
    {
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
            builder => {
                // Run other builders first
                next(builder);
                builder.AdditionalHandlers.Insert(0, new FusionHttpMessageHandler());
            };
    }
}
