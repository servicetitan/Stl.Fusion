using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // TODO: configure model binder providers
    }

    // TODO: add AddControllers and AddControllerFilter
}
