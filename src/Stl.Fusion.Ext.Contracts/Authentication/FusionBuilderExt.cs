namespace Stl.Fusion.Authentication;

public static class FusionBuilderExt
{
    public static FusionBuilder AddAuthClient(this FusionBuilder fusion)
        => fusion.AddComputeClient<IAuth>();
}
