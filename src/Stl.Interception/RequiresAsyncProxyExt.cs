using Stl.Interception.Internal;

namespace Stl.Interception;

public static class RequiresAsyncProxyExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IProxy RequireProxy(this IRequiresAsyncProxy? source)
        => source.RequireProxy<IProxy>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TProxy RequireProxy<TProxy>(this IRequiresAsyncProxy? source)
        => source is TProxy expected
            ? expected
            : throw Errors.InvalidProxyType(source?.GetType(), typeof(InterfaceProxy));
}
