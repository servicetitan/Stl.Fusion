namespace Stl.Interception;

public static class GenerateProxyHelper
{
    public static readonly BindingFlags GetMethodBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static MethodInfo? GetMethodInfo(Type type, string name, Type[] types)
    {
#if NETSTANDARD || NETCOREAPP
        return type.GetMethod(name, GetMethodBindingFlags, null, types, null);
#else
        return type.GetMethod(name, GetMethodBindingFlags, types);
#endif
    }
}
