namespace Stl.Interception;

public static class ProxyHelper
{
    private static readonly BindingFlags GetMethodBindingFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo GetMethodInfo(Type type, string name, Type[] types)
    {
#if NETSTANDARD || NETCOREAPP
        var result = type.GetMethod(name, GetMethodBindingFlags, null, types, null);
#else
        var result = type.GetMethod(name, GetMethodBindingFlags, types);
#endif
        return result ?? throw new MissingMethodException(type.Name, name);
    }
}
