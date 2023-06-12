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
    private class ControllersAddedTag { }
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());
    private static readonly ServiceDescriptor ControllersAddedTagDescriptor = new(typeof(ControllersAddedTag), new ControllersAddedTag());

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
        fusion.Rpc.UseWebSocketServer();

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

    public FusionWebServerBuilder AddAuthentication(
        Func<IServiceProvider, ServerAuthHelper.Options>? optionsFactory = null)
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => ServerAuthHelper.Options.Default);
        if (services.HasService<ServerAuthHelper>())
            return this;

        services.AddScoped<ServerAuthHelper>();
        services.AddSingleton<AuthSchemasCache>();
        AddSessionMiddleware();
        AddControllers();
        return this;
    }

    public FusionWebServerBuilder AddSessionMiddleware(
        Func<IServiceProvider, SessionMiddleware.Options>? optionsFactory = null)
    {
        var services = Services;
        services.AddSingleton(optionsFactory, _ => SessionMiddleware.Options.Default);
        Services.TryAddScoped<SessionMiddleware>();
        return this;
    }

    public FusionWebServerBuilder AddControllers(
        Func<IServiceProvider, SignInController.Options>? signInControllerOptionsFactory = null)
    {
        var services = Services;
        services.AddSingleton(signInControllerOptionsFactory, _ => SignInController.Options.Default);
        if (services.Contains(ControllersAddedTagDescriptor))
            return this;

        services.Add(ControllersAddedTagDescriptor);
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
