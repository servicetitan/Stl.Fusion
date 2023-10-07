using Stl.Internal;

namespace Stl.DependencyInjection;

public static class HasWhenDisposedExt
{
    public static void ThrowIfDisposedOrDisposing(this IHasWhenDisposed target)
    {
        if (target.WhenDisposed != null)
            throw Errors.AlreadyDisposedOrDisposing(target.GetType());
    }
}
