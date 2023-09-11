namespace Stl.Interception.Interceptors;

public abstract class MethodDef
{
    private static readonly MethodInfo InvokeMethod =
        typeof(MethodDef).GetMethod(nameof(Invoke), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly ConcurrentDictionary<Type, Func<MethodDef, object, ArgumentList, Task>> InvokerCache = new();

    private string? _fullName;
    private Func<object, ArgumentList, Task>? _invoker;

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
    public Type UnwrappedReturnType { get; }
    public Func<object, ArgumentList, Task> Invoker => _invoker ??= CreateInvoker();

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
            IsAsyncVoidMethod = false;
        }
        UnwrappedReturnType = IsAsyncMethod
            ? IsAsyncVoidMethod ? typeof(Unit) : returnType.GetGenericArguments()[0]
            : returnType;
    }

    public override string ToString()
        => $"{GetType().Name}({FullName}){(IsValid ? "" : " - invalid")}";

    // Private methods

    private Func<object, ArgumentList, Task> CreateInvoker()
    {
        var staticInvoker = InvokerCache.GetOrAdd(UnwrappedReturnType,
            tResult => (Func<MethodDef, object, ArgumentList, Task>)InvokeMethod
                .MakeGenericMethod(tResult)
                .CreateDelegate(typeof(Func<MethodDef, object, ArgumentList, Task>)));
        return (service, arguments) => staticInvoker.Invoke(this, service, arguments);
    }

    private static Task Invoke<TResult>(MethodDef methodDef, object service, ArgumentList arguments)
    {
        var result = arguments.GetInvoker(methodDef.Method).Invoke(service, arguments);
        if (methodDef.ReturnsTask) {
            var task = (Task)result!;
            if (methodDef.IsAsyncVoidMethod)
                return task.IsCompletedSuccessfully() ? TaskExt.UnitTask : ToUnitTask(task);
            return task;
        }

        if (methodDef.ReturnsValueTask) {
            if (result is ValueTask<TResult> valueTask)
                return valueTask.AsTask();
            if (result is ValueTask voidValueTask)
                return voidValueTask.IsCompletedSuccessfully ? TaskExt.UnitTask : ToUnitTask(voidValueTask);
        }

        return Task.FromResult((TResult)result!);
    }

    private static async Task<Unit> ToUnitTask(Task source)
    {
        await source.ConfigureAwait(false);
        return default;
    }

    private static async Task<Unit> ToUnitTask(ValueTask source)
    {
        await source.ConfigureAwait(false);
        return default;
    }
}
