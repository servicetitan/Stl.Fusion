using System;

namespace Stl.ImmutableModel.Updating
{
    [Flags]
    public enum NodeChangeType
    {
        Removed = 0x1,
        Created = 0x2,
        PropertyChanged = 0x4,
        TypeChanged = 0x8,
        SubtreeChanged = 0x10,
        Any = Created | Removed | PropertyChanged | SubtreeChanged,
    }
}
