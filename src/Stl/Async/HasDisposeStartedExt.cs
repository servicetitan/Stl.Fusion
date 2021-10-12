using Stl.Internal;

namespace Stl.Async;

public static class HasDisposeStartedExt
{
    public static void ThrowIfDisposedOrDisposing(this IHasDisposeStarted target)
    {
        if (target.IsDisposeStarted)
            throw Errors.AlreadyDisposedOrDisposing();
    }
}
