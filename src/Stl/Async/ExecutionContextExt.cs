namespace Stl.Async;

public static class ExecutionContextExt
{
    public static readonly ExecutionContext Default;

    [ThreadStatic] private static Func<Task>? _taskFactory0;
    [ThreadStatic] private static Func<object?, Task>? _taskFactory1;
    [ThreadStatic] private static Task? _task;

    static ExecutionContextExt()
    {
        var tContext = typeof(ExecutionContext);
#if NETSTANDARD2_0 || NETSTANDARD2_1
        const string fDefaultName = "s_dummyDefaultEC";
#else
        const string fDefaultName = "Default";
#endif
        Default = (ExecutionContext)tContext
            .GetField(fDefaultName, BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
    }

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
