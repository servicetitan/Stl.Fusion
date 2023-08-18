namespace Stl.Fusion.Blazor;

public static class FusionBuilderExt
{
    public static FusionBlazorBuilder AddBlazor(this FusionBuilder fusion)
        => new(fusion, null);

    public static FusionBuilder AddBlazor(this FusionBuilder fusion,
        Action<FusionBlazorBuilder>? configure)
        => new FusionBlazorBuilder(fusion, configure).Fusion;
}
