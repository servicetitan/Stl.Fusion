using Stl.Reflection;

namespace Stl.Messaging;

public interface IMessageProcess
{
    object UntypedMessage { get; }
    CancellationToken CancellationToken { get; }
    Task<Unit> WhenStarted { get; }
    Task<object?> WhenCompleted { get; }

    void MarkStarted();
    void MarkCompleted(Result<object?> result);
    void MarkCompletedAfter(Task<object?> resultTask);
    void MarkFailed(Exception error);
}

public interface IMessageProcess<out TMessage> : IMessageProcess
{
    public TMessage Message { get; }
}

public abstract class MessageProcess : IMessageProcess
{
    private static readonly ConcurrentDictionary<Type,
        Func<object, CancellationToken, Task<Unit>?, Task<object?>?, object>> MessageProcessorCtorCache = new();

    public abstract object UntypedMessage { get; }
    public CancellationToken CancellationToken { get; protected init; }
    public Task<Unit> WhenStarted { get; protected init; } = null!;
    public Task<object?> WhenCompleted { get; protected init; } = null!;

    public abstract void MarkStarted();
    public abstract void MarkCompleted(Result<object?> result);
    public abstract void MarkCompletedAfter(Task<object?> resultTask);
    public abstract void MarkFailed(Exception error);

    public static IMessageProcess New(
        object message,
        CancellationToken cancellationToken,
        Task<Unit>? whenStarted = null,
        Task<object?>? whenCompleted = null)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        var ctor = MessageProcessorCtorCache.GetOrAdd(
            message.GetType(),
            t => (Func<object, CancellationToken, Task<Unit>?, Task<object?>?, object>)
                typeof(MessageProcess<>)
                    .MakeGenericType(t)
                    .GetConstructorDelegate(typeof(object), typeof(CancellationToken), typeof(Task<Unit>), typeof(Task<object>))!);
        return (IMessageProcess)ctor.Invoke(message, cancellationToken, whenStarted, whenCompleted);
    }
}

public class MessageProcess<TMessage> : MessageProcess, IMessageProcess<TMessage>
    where TMessage : class
{
    public TMessage Message { get; }
    public override object UntypedMessage => Message;

    public MessageProcess(
        object message,
        CancellationToken cancellationToken,
        Task<Unit>? whenStarted = null,
        Task<object?>? whenCompleted = null)
    {
        Message = (TMessage)message;
        CancellationToken = cancellationToken;
        WhenStarted = whenStarted ?? TaskSource.New<Unit>(true).Task;
        WhenCompleted = whenCompleted ?? TaskSource.New<object?>(true).Task;
    }

    public MessageProcess(
        TMessage message,
        CancellationToken cancellationToken,
        Task<Unit>? whenStarted = null,
        Task<object?>? whenCompleted = null)
    {
        Message = message;
        CancellationToken = cancellationToken;
        WhenStarted = whenStarted ?? TaskSource.New<Unit>(true).Task;
        WhenCompleted = whenCompleted ?? TaskSource.New<object?>(true).Task;
    }

    public override void MarkStarted()
    {
        var whenStarted = TaskSource.For(WhenStarted);
        whenStarted.TrySetResult(default);
    }

    public override void MarkCompleted(Result<object?> result)
    {
        var whenStarted = TaskSource.For(WhenStarted);
        var whenCompleted = TaskSource.For(WhenCompleted);
        whenStarted.TrySetResult(default);
        whenCompleted.TrySetFromResult(result, CancellationToken);
    }

    public override void MarkCompletedAfter(Task<object?> resultTask)
        => resultTask.ContinueWith(async t => {
            try {
                var result = await t.ConfigureAwait(false);
                MarkCompleted(result);
            }
            catch (Exception e) {
                MarkFailed(e);
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public override void MarkFailed(Exception error)
    {
        var whenStarted = TaskSource.For(WhenStarted);
        var whenCompleted = TaskSource.For(WhenCompleted);
        if (error is OperationCanceledException && CancellationToken.IsCancellationRequested) {
            whenStarted.TrySetCanceled(CancellationToken);
            whenCompleted.TrySetCanceled(CancellationToken);
        }
        else {
            whenStarted.TrySetException(error);
            whenCompleted.TrySetException(error);
        }
    }
}
