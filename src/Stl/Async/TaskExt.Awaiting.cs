using Stl.Async.Internal;

namespace Stl.Async;

public static partial class TaskExt
{
    // VoidAwait

    public static VoidTaskAwaiter<TTask> SilentAwait<TTask>(this TTask task, bool captureContext = true)
        where TTask : Task
        => new(task, captureContext);
    public static VoidValueTaskAwaiter SilentAwait(this ValueTask task, bool captureContext = true)
        => new(task, captureContext);
    public static VoidValueTaskAwaiter<T> SilentAwait<T>(this ValueTask<T> task, bool captureContext = true)
        => new(task, captureContext);

    // ResultAwait

    public static ResultTaskAwaiter ResultAwait(this Task task, bool captureContext = true)
        => new(task, captureContext);
    public static ResultTaskAwaiter<T> ResultAwait<T>(this Task<T> task, bool captureContext = true)
        => new(task, captureContext);
    public static ResultValueTaskAwaiter ResultAwait(this ValueTask task, bool captureContext = true)
        => new(task, captureContext);
    public static ResultValueTaskAwaiter<T> ResultAwait<T>(this ValueTask<T> task, bool captureContext = true)
        => new(task, captureContext);

    // SuppressCancellationAwait

    public static SuppressCancellationTaskAwaiter SuppressCancellationAwait(this Task task, bool captureContext = true)
        => new(task, captureContext);
    public static SuppressCancellationTaskAwaiter<T> SuppressCancellationAwait<T>(this Task<T> task, bool captureContext = true)
        => new(task, captureContext);
    public static SuppressCancellationValueTaskAwaiter SuppressCancellationAwait(this ValueTask task, bool captureContext = true)
        => new(task, captureContext);
    public static SuppressCancellationValueTaskAwaiter<T> SuppressCancellationAwait<T>(this ValueTask<T> task, bool captureContext = true)
        => new(task, captureContext);
}
