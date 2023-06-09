using Stl.Internal;

namespace Stl.Locking.Internal;

public sealed class AsyncLockReentryCounter
{
    public int Value;

    public AsyncLockReentryCounter(int value)
        => Value = value;

    public bool Enter(LockReentryMode reentryMode)
    {
        var value = ++Value;
        if (reentryMode == LockReentryMode.CheckedFail && value > 1) {
            Value--;
            throw Errors.AlreadyLocked();
        }
        return value == 1;
    }

    public bool Leave()
    {
        var value = --Value;
        if (value < 0)
            throw Errors.InternalError($"{nameof(AsyncLockReentryCounter)}'s value is < 0.");
        return value == 0;
    }
}
