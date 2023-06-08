using Stl.Internal;

namespace Stl.Locking.Internal;

public sealed class AsyncLockReentryCounter
{
    private int _value;

    public AsyncLockReentryCounter(int value)
        => _value = value;

    public bool Enter()
    {
        return Interlocked.Increment(ref _value) == 1;
    }

    public bool Leave()
    {
        var value = Interlocked.Decrement(ref _value);
        if (value < 0)
            throw Errors.InternalError($"{nameof(AsyncLockReentryCounter)}'s value is < 0.");
        return value == 0;
    }
}
