using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Stl.DependencyInjection;

namespace Stl.Fusion.Client.RestEase.Internal
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class FusionHttpMessageHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter, IHasServices
    {
        public IServiceProvider Services { get; }

        public FusionHttpMessageHandlerBuilderFilter(IServiceProvider services)
            => Services = services;

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
            => builder => {
                // Run other builders first
                next(builder);
                builder.AdditionalHandlers.Insert(0, Services.GetRequiredService<FusionHttpMessageHandler>());
            };
    }
}
