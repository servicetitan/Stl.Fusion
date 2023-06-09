namespace Stl.Locking.Internal;

public sealed class AsyncLockReentryCounter
{
    public int Value;

    public AsyncLockReentryCounter(int value = 0)
        => Value = value;
}
