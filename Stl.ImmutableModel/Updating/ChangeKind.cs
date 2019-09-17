using System;

namespace Stl.ImmutableModel.Updating
{
    [Flags]
    public enum NodeChangeType
    {
        InstanceChanged = 0x1,
        Created = 0x3,
        Removed = 0x5,
        PropertyChanged = 0x9,
        SubtreeChanged = 0x11,
        Any = InstanceChanged | Created | Removed | PropertyChanged | SubtreeChanged,
    }
}
