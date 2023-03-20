using Stl.Interception.Internal;

namespace Stl.Interception;

public readonly record struct Invocation(
    object Proxy,
    object? ProxyTarget,
    MethodInfo Method,
    ArgumentList Arguments,
    Delegate InterceptedDelegate)
{
    public void Intercepted()
    {
        if (InterceptedDelegate is Action<ArgumentList> action)
            action.Invoke(Arguments);
        else
            throw Errors.InvalidInterceptedDelegate();
    }

    public TResult Intercepted<TResult>()
    {
        return InterceptedDelegate is Func<ArgumentList, TResult> func
            ? func.Invoke(Arguments)
            : throw Errors.InvalidInterceptedDelegate();
    }
};
