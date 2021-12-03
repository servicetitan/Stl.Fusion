namespace Stl.Reflection.Internal;

public static class Errors
{
    public static Exception PropertyOrFieldInfoExpected(string paramName)
        => new ArgumentException("PropertyInfo or FieldInfo expected.", paramName);

}
