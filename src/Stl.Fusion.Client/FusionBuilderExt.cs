using Microsoft.Extensions.Http;

namespace Stl.Fusion.Client;

public static class FusionBuilderExt
{
    public static FusionRestEaseClientBuilder AddRestEaseClient(this FusionBuilder fusion)
        => new(fusion, null);

    public static FusionBuilder AddRestEaseClient(this FusionBuilder fusion, Action<FusionRestEaseClientBuilder> configure)
        => new FusionRestEaseClientBuilder(fusion, configure).Fusion;
}
