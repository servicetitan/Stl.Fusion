using System;

namespace Stl.Purifier
{
    [Flags]
    public enum ComputeOptions
    {
        TryGetCached = 1,
        Invalidate = 2 + TryGetCached,
        Capture = 4,
    }
}
