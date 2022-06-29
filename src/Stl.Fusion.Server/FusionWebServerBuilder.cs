using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Server.Authentication;
using Stl.Fusion.Server.Controllers;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server;

public readonly struct FusionWebServerBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor =
        new(typeof(AddedTag), new AddedTag());

    public FusionBuilder Fusion { get; }
    public IServiceCollection Services => Fusion.Services;

    internal FusionWebServerBuilder(FusionBuilder fusion,
        Func<IServiceProvider, WebSocketServer.Options>? webSocketServerOptionsFactory)
    {
        Fusion = fusion;
        if (Services.Contains(AddedTagDescriptor))
            return;
        // We want above Contains call to run in O(1), so...
        Services.Insert(0, AddedTagDescriptor);

        Fusion.AddPublisher();
        Services.TryAddSingleton(c => webSocketServerOptionsFactory?.Invoke(c) ?? new());
        Services.TryAddSingleton<WebSocketServer>();
        Services.TryAddSingleton<SessionMiddleware.Options>();
        Services.TryAddScoped<SessionMiddleware>();

        var mvcBuilder = Services.AddMvcCore(options => {
            var oldModelBinderProviders = options.ModelBinderProviders.ToList();
            var newModelBinderProviders = new IModelBinderProvider[] {
                new SimpleModelBinderProvider<Moment, MomentModelBinder>(),
                new SimpleModelBinderProvider<Symbol, SymbolModelBinder>(),
                new SimpleModelBinderProvider<Session, SessionModelBinder>(),
                new PageRefModelBinderProvider(),
                new RangeModelBinderProvider(),
            };
            options.ModelBinderProviders.Clear();
            options.ModelBinderProviders.AddRange(newModelBinderProviders);
            options.ModelBinderProviders.AddRange(oldModelBinderProviders);
        });

        // Newtonsoft.Json serializer is optional starting from v1.4+
        /*
        mvcBuilder.AddNewtonsoftJson(options => {
            MemberwiseCopier.Invoke(
                NewtonsoftJsonSerializer.DefaultSettings,
                options.SerializerSettings,
                copier => copier with {
                    Filter = member => member.Name != "Binder",
                });
        });
        */
    }

    public FusionWebServerBuilder SetupSessionMiddleware(
        Func<IServiceProvider, SessionMiddleware.Options> sessionMiddlewareOptionsFactory)
    {
        Services.AddSingleton(sessionMiddlewareOptionsFactory);
        return this;
    }

    public FusionWebServerBuilder AddControllers(
        Func<IServiceProvider, SignInController.Options>? signInControllerOptionsFactory = null)
    {
        Services.TryAddSingleton(c => signInControllerOptionsFactory?.Invoke(c) ?? new());
        Services.AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly);
        return this;
    }

    public FusionWebServerBuilder AddControllerFilter(Func<TypeInfo, bool> controllerFilter)
    {
        Services.AddControllers()
            .ConfigureApplicationPartManager(m => m.FeatureProviders.Add(
                new ControllerFilter(controllerFilter)));
        return this;
    }
}
