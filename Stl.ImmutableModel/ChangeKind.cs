using System;

namespace Stl.ImmutableModel
{
    [Flags]
    public enum ChangeKind
    {
        Changed = 0x1,
        Added = 0x2,
        Removed = 0x4,
        SubtreeChanged = 0x10,
    }
}
