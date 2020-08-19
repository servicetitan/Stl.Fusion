using System;

namespace Stl.Fusion
{
    [Flags]
    public enum CallOptions
    {
        TryGetExisting = 1,
        Invalidate = 2 + TryGetExisting,
        Capture = 4,
    }
}
