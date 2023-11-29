namespace Stl.Async;

public static class ExecutionContextExt
{
    private const string DefaultFieldName
#if NETSTANDARD2_0 || NETSTANDARD2_1
        = "s_dummyDefaultEC";
#else
        = "Default";
#endif
    [ThreadStatic] private static Func<Task>? _taskFactory0;
    [ThreadStatic] private static Func<object?, Task>? _taskFactory1;
    [ThreadStatic] private static Task? _task;

    public static readonly ExecutionContext Default
#if NET8_0_OR_GREATER
        = DefaultGetter(null!);

    [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = DefaultFieldName)]
    private static extern ref ExecutionContext DefaultGetter(ExecutionContext @this);
#else
        = (ExecutionContext)typeof(ExecutionContext)
            .GetField(DefaultFieldName, BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
#endif

#if NET8_0_OR_GREATER
    public static AsyncFlowControl TrySuppressFlow()
        => ExecutionContext.IsFlowSuppressed()
            ? default
            : ExecutionContext.SuppressFlow();
#else
    public static ClosedDisposable<AsyncFlowControl> TrySuppressFlow()
    {
        if (ExecutionContext.IsFlowSuppressed())
            return default;

        var releaser = ExecutionContext.SuppressFlow();
        return Disposable.NewClosed(releaser, r => r.Dispose());
    }
#endif

    // Start

    public static Task Start(
        ExecutionContext executionContext,
        Func<Task> taskFactory)
    {
        var oldTask = _task;
        try {
            _taskFactory0 = taskFactory;
            ExecutionContext.Run(executionContext, static _ => _task = _taskFactory0.Invoke(), null);
            return _task!;
        }
        finally {
            _task = oldTask;
        }
    }

    public static Task Start(
        ExecutionContext executionContext,
        Func<object?, Task> taskFactory,
        object? state = null)
    {
        var oldTask = _task;
        try {
            _taskFactory1 = taskFactory;
            ExecutionContext.Run(executionContext, static state => _task = _taskFactory1.Invoke(state), state);
            return _task!;
        }
        finally {
            _task = oldTask;
        }
    }

    public static Task<T> Start<T>(
        ExecutionContext executionContext,
        Func<Task<T>> taskFactory)
    {
        var oldTask = _task;
        try {
            _taskFactory0 = taskFactory;
            ExecutionContext.Run(executionContext, static _ => _task = _taskFactory0.Invoke(), null);
            return (Task<T>)_task!;
        }
        finally {
            _task = oldTask;
        }
    }

    public static Task<T> Start<T>(
        ExecutionContext executionContext,
        Func<object?, Task<T>> taskFactory,
        object? state = null)
    {
        var oldTask = _task;
        try {
            _taskFactory1 = taskFactory;
            ExecutionContext.Run(executionContext, static state => _task = _taskFactory1.Invoke(state), state);
            return (Task<T>)_task!;
        }
        finally {
            _task = oldTask;
        }
    }
}
