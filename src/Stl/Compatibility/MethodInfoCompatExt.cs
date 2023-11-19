// ReSharper disable once CheckNamespace
namespace System.Reflection;

public static class MethodInfoCompatExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsConstructedGenericMethod(this MethodInfo method)
#if !NETSTANDARD2_0
        => method.IsConstructedGenericMethod;
#else
        => method is { IsGenericMethod: true, IsGenericMethodDefinition: false };
#endif
}
