using Microsoft.AspNetCore.Mvc.ModelBinding;
using Stl.Fusion.Server.Controllers;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server;

[StructLayout(LayoutKind.Auto)]
public readonly struct FusionMvcWebServerBuilder
{
    private class AddedTag;
    private class ControllersAddedTag;
    private static readonly ServiceDescriptor AddedTagDescriptor = new(typeof(AddedTag), new AddedTag());
    private static readonly ServiceDescriptor ControllersAddedTagDescriptor = new(typeof(ControllersAddedTag), new ControllersAddedTag());

    public FusionWebServerBuilder FusionWebServer { get; }
    public FusionBuilder Fusion => FusionWebServer.Fusion;
    public IServiceCollection Services => Fusion.Services;

    internal FusionMvcWebServerBuilder(
        FusionWebServerBuilder fusionWebServer,
        Action<FusionMvcWebServerBuilder>? configure)
    {
        FusionWebServer = fusionWebServer;
        var services = Services;
        if (services.Contains(AddedTagDescriptor)) {
            configure?.Invoke(this);
            return;
        }

        // We want above Contains call to run in O(1), so...
        services.Insert(0, AddedTagDescriptor);
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

        configure?.Invoke(this);
    }

    public FusionMvcWebServerBuilder AddControllers()
    {
        var services = Services;
        if (services.Contains(ControllersAddedTagDescriptor))
            return this;

        services.Add(ControllersAddedTagDescriptor);
        services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        return this;
    }

    public FusionMvcWebServerBuilder AddControllerFilter(Func<TypeInfo, bool> controllerFilter)
    {
        Services.AddControllers().ConfigureApplicationPartManager(
            m => m.FeatureProviders.Add(new ControllerFilter(controllerFilter)));
        return this;
    }
}
