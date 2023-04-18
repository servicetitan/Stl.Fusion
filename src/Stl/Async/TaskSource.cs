using System.Reflection.Emit;

namespace Stl.Async;

public static class TaskSource
{
    public static TaskSource<T> For<T>(Task<T> task)
        => new(task);
    public static TaskSource<T> New<T>(bool runContinuationsAsynchronously)
        => runContinuationsAsynchronously
            ? new TaskSource<T>(TaskSource<T>.CreateTask2(null, TaskCreationOptions.RunContinuationsAsynchronously))
            : new TaskSource<T>(TaskSource<T>.CreateTask0());
    public static TaskSource<T> New<T>(object? state, TaskCreationOptions taskCreationOptions)
        => new(TaskSource<T>.CreateTask2(state, taskCreationOptions));
    public static TaskSource<T> New<T>(TaskCreationOptions taskCreationOptions)
        => new(TaskSource<T>.CreateTask2(null, taskCreationOptions));
}

public readonly struct TaskSource<T> : IEquatable<TaskSource<T>>
{
    [ThreadStatic]
    private static volatile TaskCompletionSource<T>? _taskCompletionSource;
    internal static readonly Func<Task<T>> CreateTask0;
    internal static readonly Func<object?, TaskCreationOptions, Task<T>> CreateTask2;
    private static readonly Action<TaskCompletionSource<T>, Task<T>> SetTask;
    private static readonly Action<TaskCompletionSource<T>> ResetTask;

    public static TaskSource<T> Empty => default;

    public readonly Task<T> Task;

    public bool IsEmpty {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Task == null!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal TaskSource(Task<T> task) => Task = task;

    // Private methods

    private static TaskCompletionSource<T> Wrap(Task<T> task)
    {
        var tcs = _taskCompletionSource;
        if (tcs == null) {
            tcs = new TaskCompletionSource<T>();
            var oldTcs = Interlocked.CompareExchange(ref _taskCompletionSource, tcs, null);
            if (oldTcs != null)
                tcs = oldTcs;
        }
        SetTask(tcs, task);
        return tcs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySetResult(T result)
    {
        var tcs = Wrap(Task);
        try {
            return tcs.TrySetResult(result);
        }
        finally {
            ResetTask(tcs);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetResult(T result)
    {
        var tcs = Wrap(Task);
        try {
            tcs.SetResult(result);
        }
        finally {
            ResetTask(tcs);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySetException(Exception exception)
    {
        var tcs = Wrap(Task);
        try {
            return tcs.TrySetException(exception);
        }
        finally {
            ResetTask(tcs);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetException(Exception exception)
    {
        var tcs = Wrap(Task);
        try {
            tcs.SetException(exception);
        }
        finally {
            ResetTask(tcs);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySetCanceled(CancellationToken cancellationToken = default)
    {
        var tcs = Wrap(Task);
        try {
            return tcs.TrySetCanceled(cancellationToken);
        }
        finally {
            ResetTask(tcs);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCanceled()
    {
        var tcs = Wrap(Task);
        try {
            tcs.SetCanceled();
        }
        finally {
            ResetTask(tcs);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCanceled(CancellationToken cancellationToken)
    {
        var tcs = Wrap(Task);
        try {
#if NET5_0_OR_GREATER
            tcs.SetCanceled(cancellationToken);
#else
            tcs.SetCanceled();
#endif
        }
        finally {
            ResetTask(tcs);
        }
    }

    // Type initializer

    static TaskSource()
    {
        var tTcs = typeof(TaskCompletionSource<T>);
        var tTask = typeof(Task<T>);
#if !NETSTANDARD2_0
        var fTask = tTcs.GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic)!;
#else
        var fTask = tTcs.GetField("m_task", BindingFlags.Instance | BindingFlags.NonPublic)!;
#endif
        var ctorBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance;

        var taskCtor0 = tTask.GetConstructor(ctorBindingFlags, null, Type.EmptyTypes, null)!;
        var m = new DynamicMethod("_CreateTask0", typeof(Task<T>), Type.EmptyTypes, true);
        var il = m.GetILGenerator();
        il.Emit(OpCodes.Newobj, taskCtor0);
        il.Emit(OpCodes.Ret);
        CreateTask0 = (Func<Task<T>>)m.CreateDelegate(typeof(Func<Task<T>>));

        var taskCtor2Args = new [] { typeof(object), typeof(TaskCreationOptions) };
        var taskCtor2 = tTask.GetConstructor(ctorBindingFlags, null, taskCtor2Args, null)!;
        m = new DynamicMethod("_CreateTask2", typeof(Task<T>), taskCtor2Args, true);
        il = m.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Newobj, taskCtor2);
        il.Emit(OpCodes.Ret);
        CreateTask2 = (Func<object?, TaskCreationOptions, Task<T>>)m.CreateDelegate(typeof(Func<object?, TaskCreationOptions, Task<T>>));

        // .NET Standard version fails even on above code,
        // so IL emit is used to generate private field assignment code.
        m = new DynamicMethod(
            "_SetTask",
            typeof(void),
            new [] { typeof(TaskCompletionSource<T>), typeof(Task<T>) },
            fTask!.DeclaringType!,
            true);
        il = m.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        if (fTask.FieldType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, fTask.FieldType);
        il.Emit(OpCodes.Stfld, fTask);
        il.Emit(OpCodes.Ret);
        SetTask = (Action<TaskCompletionSource<T>, Task<T>>)m.CreateDelegate(
            typeof(Action<TaskCompletionSource<T>, Task<T>>));

        var resetTaskMethod = new DynamicMethod(
            "_ResetTask",
            typeof(void),
            new [] { typeof(TaskCompletionSource<T>) },
            fTask.DeclaringType!,
            true);
        il = resetTaskMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Stfld, fTask);
        il.Emit(OpCodes.Ret);
        ResetTask = (Action<TaskCompletionSource<T>>) resetTaskMethod.CreateDelegate(
            typeof(Action<TaskCompletionSource<T>>));
    }

    // Equality

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(TaskSource<T> other)
        => Task.Equals(other.Task);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
        => obj is TaskSource<T> other && Equals(other);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
        => Task.GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(TaskSource<T> left, TaskSource<T> right)
        => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(TaskSource<T> left, TaskSource<T> right)
        => !left.Equals(right);
}
