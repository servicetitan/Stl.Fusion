namespace Stl.Async;

public static class CancellationTokenExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CancellationTokenSource LinkWith(this CancellationToken token1, CancellationToken token2)
        => CancellationTokenSource.CreateLinkedTokenSource(token1, token2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CancellationTokenSource CreateLinkedTokenSource(this CancellationToken cancellationToken)
        => CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    // ToTask

    public static Disposable<Task, CancellationTokenRegistration> ToTask(
        this CancellationToken token,
        TaskCreationOptions taskCreationOptions = default)
    {
        var ts = TaskSource.New<Unit>(taskCreationOptions);
        var r = token.Register(() => ts.TrySetCanceled(token));
#if NETSTANDARD
        return Disposable.New((Task) ts.Task, r, (t, r1) => {
            r1.Dispose();
            TaskSource.For((Task<Unit>) t).TrySetCanceled();
        });
#else
        return Disposable.New((Task) ts.Task, r, (t, r1) => {
            r1.Unregister();
            TaskSource.For((Task<Unit>) t).TrySetCanceled();
        });
#endif
    }

    public static Disposable<Task<T>, CancellationTokenRegistration> ToTask<T>(
        this CancellationToken token,
        TaskCreationOptions taskCreationOptions = default)
    {
        var ts = TaskSource.New<T>(taskCreationOptions);
        var r = token.Register(() => ts.TrySetCanceled(token));
#if NETSTANDARD
        return Disposable.New(ts.Task, r, (t, r1) => {
            r1.Dispose();
            TaskSource.For(t).TrySetCanceled();
        });
#else
        return Disposable.New(ts.Task, r, (t, r1) => {
            r1.Unregister();
            TaskSource.For(t).TrySetCanceled();
        });
#endif
    }

    // ToTaskUnsafe

    internal static Task ToTaskUnsafe(
        this CancellationToken token, 
        TaskCreationOptions taskCreationOptions = default)
    {
        var ts = TaskSource.New<Unit>(taskCreationOptions);
        token.Register(() => ts.TrySetCanceled(token));
        return ts.Task;
    }

    internal static Task<T> ToTaskUnsafe<T>(
        this CancellationToken token,
        TaskCreationOptions taskCreationOptions = default)
    {
        var ts = TaskSource.New<T>(taskCreationOptions);
        token.Register(() => ts.TrySetCanceled(token));
        return ts.Task;
    }
}
