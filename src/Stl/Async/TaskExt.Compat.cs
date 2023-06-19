namespace Stl.Async;

public static partial class TaskExt
{
    /// <summary>
    /// Cross-platform version of <code>IsCompletedSuccessfully</code> from .NET Core.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <returns>True if <paramref name="task"/> is completed successfully; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCompletedSuccessfully(this Task task)
    {
#if NETSTANDARD2_0
        return task.Status == TaskStatus.RanToCompletion;
#else
        return task.IsCompletedSuccessfully;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFaultedOrCancelled(this Task task)
        => task.Status is TaskStatus.Faulted or TaskStatus.Canceled;
}
