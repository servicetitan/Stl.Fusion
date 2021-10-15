namespace Stl.Fusion.Bridge;

public static class FusionHeaders
{
    public const string CommonPrefix = "X-Fusion-";
    public const string RequestPublication = "X-Fusion-Publish"; // Must be const string to be used in attribute
    public static readonly string Publication = $"{CommonPrefix}{nameof(Publication)}";
}
