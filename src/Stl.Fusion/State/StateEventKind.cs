using System;

namespace Stl.Fusion
{
    [Flags]
    public enum StateEventKind
    {
        Invalidated = 1,
        Updating = 2,
        Updated = 4,
        All = Invalidated | Updating | Updated,
    }
}
