using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public interface IRpcOutboundStreamCall
{
    Func<ArgumentList> ItemResultListFactory { get; }
    void AddItem(object? item);
}

public class RpcOutboundStreamCall<TItem> : RpcOutboundCall, IRpcOutboundStreamCall
{
    protected static readonly Func<ArgumentList> StaticItemResultListFactory = () => new ArgumentList<TItem>(default!);
    protected readonly TaskCompletionSource<Channel<TItem>?> StreamSource;

    public Func<ArgumentList> ItemResultListFactory { get; } = StaticItemResultListFactory;

    public RpcOutboundStreamCall(RpcOutboundContext context)
        : base(context)
    {
        if (NoWait)
            throw new ArgumentOutOfRangeException(nameof(context));
        StreamSource = new TaskCompletionSource<Channel<TItem>?>();
        ResultTask = StreamSource.Task;
    }

    public void AddItem(object? item)
    {
        throw new NotImplementedException();
    }

    public override void SetResult(object? result, RpcInboundContext? context)
    {
        var typedResult = false;
        try {
            if (result != null)
                typedResult = (bool)result;
        }
        catch (InvalidCastException) {
            // Intended
        }
        if (StreamSource.TrySetResult(typedResult)) {
            Unregister();
            if (context != null && Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.ResultSource?.TrySetResult(context.Message.ArgumentData);
        }
    }

    public override void SetError(Exception error, RpcInboundContext? context, bool notifyCancelled = false)
    {
        if (StreamSource.TrySetException(error)) {
            Unregister(notifyCancelled);
            if (Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.ResultSource?.TrySetException(error);
        }
    }

    public override bool SetCancelled(CancellationToken cancellationToken, RpcInboundContext? context)
    {
        var isCancelled = StreamSource.TrySetCanceled(cancellationToken);
        if (isCancelled) {
            Unregister(true);
            if (Context.CacheInfoCapture is { } cacheInfoCapture)
                cacheInfoCapture.ResultSource?.TrySetCanceled(cancellationToken);
        }
        return isCancelled;
    }
}
