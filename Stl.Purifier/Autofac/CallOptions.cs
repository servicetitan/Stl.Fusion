using System;

namespace Stl.Purifier.Autofac
{
    [Flags]
    public enum CallOptions
    {
        Capture = 1,
        CachedOnly = 2,
        Invalidate = 4 + CachedOnly,
    }
}
