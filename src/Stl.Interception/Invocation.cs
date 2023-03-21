using Stl.Interception.Internal;
using Stl.OS;

namespace Stl.Interception;

public readonly record struct Invocation(
    object Proxy,
    MethodInfo Method,
    ArgumentList Arguments,
    Delegate InterceptedDelegate)
{
    private static readonly MethodInfo InterceptedUntypedMethod = typeof(Invocation)
        .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .Single(m => StringComparer.Ordinal.Equals(m.Name, nameof(InterceptedUntyped)));
    private static readonly ConcurrentDictionary<Type, Func<Invocation, object?>> InterceptedUntypedCache 
        = new(HardwareInfo.GetProcessorCountPo2Factor(4), 256);

    public object? ProxyTarget => (Proxy as InterfaceProxy)?.ProxyTarget;

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

    public object? InterceptedUntyped()
        => InterceptedUntypedCache
            .GetOrAdd(Method.ReturnType, static returnType => {
                return returnType == typeof(void)
                    ? invocation => {
                        invocation.Intercepted();
                        return null;
                    }
                    : (Func<Invocation, object?>)InterceptedUntypedMethod
                        .MakeGenericMethod(returnType)
                        .CreateDelegate(typeof(Func<Invocation, object?>));
            }).Invoke(this);

    // Private methods

    private static object? InterceptedUntyped<TResult>(Invocation invocation)
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => invocation.InterceptedDelegate is Func<ArgumentList, TResult> func
            ? func.Invoke(invocation.Arguments)
            : throw Errors.InvalidInterceptedDelegate();
};
