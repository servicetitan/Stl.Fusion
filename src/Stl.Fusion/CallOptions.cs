namespace Stl.Fusion;

[Flags]
public enum CallOptions
{
    GetExisting = 1,
    Invalidate = 2 + GetExisting,
    Capture = 4,
}
