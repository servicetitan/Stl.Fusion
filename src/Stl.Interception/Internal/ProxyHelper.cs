namespace Stl.Interception.Internal;

public static class ProxyHelper
{
    private static readonly BindingFlags BindingFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo GetMethodInfo(Type type, string name, Type[] argumentTypes)
    {
#if NETSTANDARD || NETCOREAPP
        var result = type.GetMethod(name, BindingFlags, null, argumentTypes, null);
#else
        var result = type.GetMethod(name, GetMethodBindingFlags, types);
#endif
        return result ?? throw new MissingMethodException(type.Name, name);
    }
}
