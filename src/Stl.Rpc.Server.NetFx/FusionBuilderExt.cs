namespace Stl.Rpc.Server;

public static class FusionBuilderExt
{
    public static FusionWebServerBuilder AddWebServer(this FusionBuilder fusion)
        => new(fusion, null);

    public static FusionBuilder AddWebServer(this FusionBuilder fusion, Action<FusionWebServerBuilder> configure)
        => new FusionWebServerBuilder(fusion, configure).Fusion;
}
