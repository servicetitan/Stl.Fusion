using Stl.Interception;
using Stl.Internal;

namespace Stl.Rpc.Infrastructure;

public interface IRpcOutboundCall : IRpcCall
{
    RpcOutboundContext Context { get; }
    long Id { get; }
    Task ResultTask { get; }

    RpcMessage CreateMessage(long callId);
    ValueTask Send(bool isRetry = false);
    void CompleteWithOk(object? result);
    void CompleteWithError(Exception error);
}

public interface IRpcOutboundCall<TResult> : IRpcOutboundCall
{ }

public class RpcOutboundCall<TResult> : RpcCall<TResult>, IRpcOutboundCall<TResult>
{
    private readonly TaskCompletionSource<TResult> _resultSource;

    public RpcOutboundContext Context { get; }
    public long Id { get; protected set; }
    public Task ResultTask => _resultSource.Task;

    public RpcOutboundCall(RpcOutboundContext context) : base(context.MethodDef!)
    {
        Context = context;
        _resultSource = context.MethodDef!.NoWait
            ? (TaskCompletionSource<TResult>)(object)RpcNoWait.TaskSources.Completed
            : TaskCompletionSourceExt.New<TResult>();
    }

    public virtual ValueTask Send(bool isRetry = false)
    {
        var peer = Context.Peer!;
        RpcMessage message;
        if (Context.MethodDef!.NoWait)
            message = CreateMessage(Context.RelatedCallId);
        else if (Id == 0) {
            Id = peer.Calls.NextId;
            message = CreateMessage(Id);
            peer.Calls.Outbound.TryAdd(Id, this);
        }
        else
            message = CreateMessage(Id);
        return peer.Send(message, Context.CancellationToken);
    }

    public virtual void CompleteWithOk(object? result)
    {
        try {
            if (_resultSource.TrySetResult((TResult)result!))
                Context.Peer!.Calls.Outbound.TryRemove(Id, this);
        }
        catch (Exception e) {
            CompleteWithError(e);
        }
    }

    public virtual void CompleteWithError(Exception error)
    {
        if (_resultSource.TrySetException(error))
            Context.Peer!.Calls.Outbound.TryRemove(Id, this);
    }

    public virtual RpcMessage CreateMessage(long callId)
    {
        var headers = Context.Headers;
        var peer = Context.Peer!;
        var arguments = Context.Arguments!;
        if (MethodDef.CancellationTokenIndex >= 0)
            arguments = arguments.Remove(MethodDef.CancellationTokenIndex);

        var argumentListType = arguments.GetType();
        if (argumentListType.IsGenericType) {
            var nonDefaultItemTypes = arguments.GetNonDefaultItemTypes();
            if (nonDefaultItemTypes != null) {
                var gParameters = argumentListType.GetGenericArguments();
                for (var i = 0; i < nonDefaultItemTypes.Length; i++) {
                    var itemType = nonDefaultItemTypes[i];
                    if (itemType == null)
                        continue;

                    gParameters[i] = itemType;
                    var typeRef = new TypeRef(itemType);
                    var h = new RpcHeader(RpcHeader.ArgumentTypeHeaders[i], typeRef.AssemblyQualifiedName);
                    headers.Add(h);
                }
                argumentListType = argumentListType
                    .GetGenericTypeDefinition()
                    .MakeGenericType(gParameters);
                var oldArguments = arguments;
                arguments = (ArgumentList)argumentListType.CreateInstance();
                arguments.SetFrom(oldArguments);
            }
        }
        return peer.ArgumentSerializer.CreateMessage(callId, MethodDef, arguments, headers);
    }
}
