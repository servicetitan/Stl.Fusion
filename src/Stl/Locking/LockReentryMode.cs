namespace Stl.Locking;

public enum LockReentryMode
{
    Unchecked = 0,
    CheckedFail,
    CheckedPass,
}
