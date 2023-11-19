using System.Diagnostics.CodeAnalysis;

namespace Stl.Interception.Internal;

public static class ProxyHelper
{
    private static readonly BindingFlags GetMethodInfoBindingFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodInfo GetMethodInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type,
        string name, Type[] argumentTypes)
    {
        var result = GetMethodInfoImpl(type, name, argumentTypes);
        if (result != null)
            return result;

        if (type.IsInterface) {
            foreach (var tInterface in type.GetAllBaseTypes(false, true)) {
#pragma warning disable IL2072
                result = GetMethodInfoImpl(tInterface, name, argumentTypes);
#pragma warning restore IL2072
                if (result != null)
                    return result;
            }
        }

        throw new MissingMethodException(type.Name, name);
    }

    private static MethodInfo? GetMethodInfoImpl(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type,
        string name, Type[] argumentTypes)
    {
#if NETSTANDARD || NETCOREAPP
        return type.GetMethod(name, GetMethodInfoBindingFlags, null, argumentTypes, null);
#else
        return type.GetMethod(name, GetMethodInfoBindingFlags, types);
#endif
    }
}
