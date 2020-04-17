using System;

namespace Stl.Purifier.Autofac
{
    [Flags]
    public enum CallAction
    {
        TryGetCached = 1,
        Invalidate = 2 + TryGetCached,
    }
}
