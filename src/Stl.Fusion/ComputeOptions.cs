using System;

namespace Stl.Fusion
{
    [Flags]
    public enum ComputeOptions
    {
        TryGetCached = 1,
        Invalidate = 2 + TryGetCached,
        Capture = 4,
    }
}
