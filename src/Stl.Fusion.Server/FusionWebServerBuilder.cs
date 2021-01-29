using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Server.Controllers;
using Stl.Reflection;
using Stl.Serialization;

namespace Stl.Fusion.Server
{
    public readonly struct FusionWebServerBuilder
    {
        private class AddedTag { }
        private static readonly ServiceDescriptor AddedTagDescriptor =
            new(typeof(AddedTag), new AddedTag());

        public FusionBuilder Fusion { get; }
        public IServiceCollection Services => Fusion.Services;

        internal FusionWebServerBuilder(FusionBuilder fusion,
            Action<IServiceProvider, FusionWebSocketServer.Options>? webSocketServerOptionsBuilder)
        {
            Fusion = fusion;
            if (Services.Contains(AddedTagDescriptor))
                return;
            // We want above Contains call to run in O(1), so...
            Services.Insert(0, AddedTagDescriptor);

            Fusion.AddPublisher();
            Services.TryAddSingleton(c => {
                var options = new FusionWebSocketServer.Options();
                webSocketServerOptionsBuilder?.Invoke(c, options);
                return options;
            });
            Services.TryAddSingleton<FusionWebSocketServer>();
            Services.AddMvcCore()
                .AddNewtonsoftJson(
                    options => MemberwiseCopier.Invoke(
                        JsonNetSerializer.DefaultSettings,
                        options.SerializerSettings));
        }

        public FusionWebServerBuilder AddControllers(
            Action<IServiceProvider, FusionSignInController.Options>? signInControllerOptionsBuilder = null)
        {
            Services.TryAddSingleton(c => {
                var options = new FusionSignInController.Options();
                signInControllerOptionsBuilder?.Invoke(c, options);
                return options;
            });
            Services.AddControllers()
                .AddApplicationPart(typeof(FusionAuthController).Assembly);
            return this;
        }
    }
}
