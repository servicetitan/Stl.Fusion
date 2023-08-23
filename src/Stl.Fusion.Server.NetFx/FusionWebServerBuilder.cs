using Stl.Rpc.Server;

namespace Stl.Fusion.Server;

public readonly struct FusionWebServerBuilder
{
    private class AddedTag;
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

        configure?.Invoke(this);
    }

    // TODO: add AddControllers and AddControllerFilter
}
