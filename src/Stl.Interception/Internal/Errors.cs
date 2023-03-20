namespace Stl.Interception.Internal;

public static class Errors
{
    public static Exception InvalidInterceptedDelegate()
        => new InvalidOperationException("Invocation.InterceptedDelegate is null or doesn't have an expected type.");
}
