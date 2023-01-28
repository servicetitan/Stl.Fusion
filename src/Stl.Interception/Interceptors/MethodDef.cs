using Castle.DynamicProxy;

namespace Stl.Interception.Interceptors;

public abstract record MethodDef
{
    private string? _fullName;

    public IInterceptor Interceptor { get; init; }
    public MethodInfo MethodInfo { get; init; }
    public string Name => MethodInfo.Name;
    public string FullName => _fullName ??= $"{MethodInfo.DeclaringType!.GetName()}.{MethodInfo.Name}";
    public bool IsAsyncMethod { get; init; }
    public bool IsAsyncVoidMethod { get; init; }
    public bool ReturnsTask { get; init; }
    public bool ReturnsValueTask { get; init; }
    public Type UnwrappedReturnType { get; init; } = null!;
    public bool IsValid { get; init; }

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

    public virtual MethodDef ToReplicaMethodDef()
        => this;
}
