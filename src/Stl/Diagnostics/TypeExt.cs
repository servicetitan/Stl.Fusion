namespace Stl.Diagnostics;

public static class TypeExt
{
    public static string GetOperationName(this Type type, string operation)
        => $"{operation}@{type.GetName()}";
}
