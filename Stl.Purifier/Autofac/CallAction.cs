using System;

namespace Stl.Purifier.Autofac
{
    [Flags]
    public enum CallAction
    {
        CaptureComputed = 1,
        TryGetCached = 2,
        Invalidate = 4 + TryGetCached,
    }
}
