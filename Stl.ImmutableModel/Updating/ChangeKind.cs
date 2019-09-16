using System;

namespace Stl.ImmutableModel.Updating
{
    [Flags]
    public enum NodeChangeType
    {
        Changed = 0x1,
        Added = 0x2,
        Removed = 0x4,
        SubtreeChanged = 0x10,
        Any = Changed | Added | Removed | SubtreeChanged,
    }
}
