using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Fusion.Server.Authentication;
using Stl.Fusion.Server.Controllers;
using Stl.Fusion.Server.Internal;
using Stl.Rpc.Server;

namespace Stl.Fusion.Server;

public readonly struct FusionWebServerBuilder
{
    private class AddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());

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
        fusion.Rpc.AddWebSocketServer();

        services.AddMvcCore(options => {
            var oldModelBinderProviders = options.ModelBinderProviders.ToList();
            var newModelBinderProviders = new IModelBinderProvider[] {
                new SimpleModelBinderProvider<Moment, MomentModelBinder>(),
                new SimpleModelBinderProvider<Symbol, SymbolModelBinder>(),
                new SimpleModelBinderProvider<Session, SessionModelBinder>(),
                new SimpleModelBinderProvider<TypeRef, TypeRefModelBinder>(),
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

        configure?.Invoke(this);
    }

    public FusionWebServerBuilder AddSessionMiddleware(
        Func<IServiceProvider, SessionMiddleware.Options>? optionsFactory = null)
    {
        if (optionsFactory != null)
            Services.AddSingleton(optionsFactory);
        else
            Services.TryAddSingleton(_ => new SessionMiddleware.Options());
        Services.TryAddScoped<SessionMiddleware>();
        return this;
    }

    public FusionWebServerBuilder AddAuthentication(
        Func<IServiceProvider, ServerAuthHelper.Options>? serverAuthHelperOptionsFactory = null,
        Func<IServiceProvider, SignInController.Options>? signInControllerOptionsFactory = null)
    {
        var services = Services;
        AddControllers(signInControllerOptionsFactory);
        if (serverAuthHelperOptionsFactory != null)
            services.AddSingleton(serverAuthHelperOptionsFactory);
        else
            services.TryAddSingleton(_ => ServerAuthHelper.Options.Default);
        services.TryAddScoped<ServerAuthHelper>();
        services.TryAddSingleton<AuthSchemasCache>();
        return this;
    }

    public FusionWebServerBuilder AddControllers(
        Func<IServiceProvider, SignInController.Options>? signInControllerOptionsFactory = null)
    {
        var services = Services;
        var isAlreadyAdded = services.HasService<SignInController.Options>();
        if (signInControllerOptionsFactory != null)
            services.AddSingleton(signInControllerOptionsFactory);
        if (isAlreadyAdded)
            return this;

        services.AddSingleton(_ => SignInController.Options.Default);
        services.AddControllers().AddApplicationPart(typeof(SignInController).Assembly);

        return this;
    }

    public FusionWebServerBuilder AddControllerFilter(Func<TypeInfo, bool> controllerFilter)
    {
        Services.AddControllers().ConfigureApplicationPartManager(
            m => m.FeatureProviders.Add(new ControllerFilter(controllerFilter)));
        return this;
    }
}
