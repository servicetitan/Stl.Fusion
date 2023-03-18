namespace Stl.Interception;

public readonly record struct Invocation(
    object? ProxyTarget,
    MethodInfo MethodInfo,
    ArgumentList Arguments,
    Delegate Delegate);
