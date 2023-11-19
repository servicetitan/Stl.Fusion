namespace Stl.Fusion;

#pragma warning disable MA0062, CA2217

[Flags]
public enum CallOptions
{
    GetExisting = 1,
    Invalidate = 2 + GetExisting,
    Capture = 4,
}
#pragma warning restore MA0062
