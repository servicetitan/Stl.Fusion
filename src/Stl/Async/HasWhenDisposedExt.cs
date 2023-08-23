using Stl.Internal;

namespace Stl.Async;

public static class HasWhenDisposedExt
{
    public static void ThrowIfDisposedOrDisposing(this IHasWhenDisposed target)
    {
        if (target.WhenDisposed != null)
            throw Errors.AlreadyDisposedOrDisposing(target.GetType());
    }
}
