using Castle.DynamicProxy;

namespace Stl.Interception.Interceptors;

public abstract class MethodDef
{
    public IInterceptor Interceptor { get; }
    public MethodInfo MethodInfo { get; }
    public bool IsAsyncMethod { get; protected init; }
    public bool IsAsyncVoidMethod { get; protected init; }
    public bool ReturnsTask { get; protected init; }
    public bool ReturnsValueTask { get; protected init; }
    public Type UnwrappedReturnType { get; protected init; } = null!;
    public bool IsValid { get; protected init; }

    protected MethodDef(
        IInterceptor interceptor,
        MethodInfo methodInfo)
    {
        Interceptor = interceptor;
        MethodInfo = methodInfo;

        var returnType = methodInfo.ReturnType;
        if (!returnType.IsGenericType) {
            ReturnsTask = returnType == typeof(Task);
            ReturnsValueTask = returnType == typeof(ValueTask);
            IsAsyncMethod = IsAsyncVoidMethod = ReturnsTask || ReturnsValueTask;
        }
        else {
            var returnTypeGtd = returnType.GetGenericTypeDefinition();
            ReturnsTask = returnTypeGtd == typeof(Task<>);
            ReturnsValueTask = returnTypeGtd == typeof(ValueTask<>);
            IsAsyncMethod = ReturnsTask || ReturnsValueTask;
        }
        UnwrappedReturnType = IsAsyncMethod && !IsAsyncVoidMethod
            ? returnType.GetGenericArguments()[0]
            : returnType;
    }
}
