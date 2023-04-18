namespace Stl.Interception.Interceptors;

public abstract class MethodDef
{
    private string? _fullName;

    public Type Type { get; }
    public MethodInfo Method { get; }
    public ParameterInfo[] Parameters { get; }
    public Type[] ParameterTypes { get; }
    public int CancellationTokenIndex { get; init; } = -1;

    public string FullName => _fullName ??= $"{Type.GetName()}.{Method.Name}";
    public bool IsAsyncMethod { get; }
    public bool IsAsyncVoidMethod { get; }
    public bool ReturnsTask { get; }
    public bool ReturnsValueTask { get; }
    public Type UnwrappedReturnType { get; } = null!;
    public bool IsValid { get; init; } = true;

    protected MethodDef(
        Type type,
        MethodInfo method)
    {
        var parameters = method.GetParameters();
        for (var i = 0; i < parameters.Length; i++) {
            var p = parameters[i];
            if (typeof(CancellationToken).IsAssignableFrom(p.ParameterType))
                CancellationTokenIndex = i;
        }
        var parameterTypes = new Type[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
            parameterTypes[i] = parameters[i].ParameterType;

        Type = type;
        Method = method;
        Parameters = parameters;
        ParameterTypes = parameterTypes;

        var returnType = method.ReturnType;
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

    public override string ToString()
        => $"{GetType().Name}({FullName}){(IsValid ? "" : " - invalid")}";
}
