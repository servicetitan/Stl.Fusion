using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcOutboundCall : RpcCall
{
    private static readonly ConcurrentDictionary<(Type, Type), Func<RpcOutboundContext, RpcOutboundCall>> FactoryCache = new();

    public RpcOutboundContext Context { get; }
    public long Id { get; protected set; }
    public Task ResultTask { get; protected init; } = null!;

    public static RpcOutboundCall New(RpcOutboundContext context)
        => FactoryCache.GetOrAdd((context.CallType, context.MethodDef!.UnwrappedReturnType), static key => {
            var (tGeneric, tResult) = key;
            var tInbound = tGeneric.MakeGenericType(tResult);
            return (Func<RpcOutboundContext, RpcOutboundCall>)tInbound.GetConstructorDelegate(typeof(RpcOutboundContext))!;
        }).Invoke(context);

    protected RpcOutboundCall(RpcOutboundContext context)
        : base(context.MethodDef!)
        => Context = context;

    public ValueTask Send(bool isRetry = false)
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

    public abstract bool TryCompleteWithOk(object? result, RpcInboundContext context);
    public abstract bool TryCompleteWithError(Exception error, RpcInboundContext? context);
    public abstract bool TryCompleteWithCancel(CancellationToken cancellationToken, RpcInboundContext? context);
}

public class RpcOutboundCall<TResult> : RpcOutboundCall
{
    private readonly TaskCompletionSource<TResult> _resultSource;

    public RpcOutboundCall(RpcOutboundContext context)
        : base(context)
    {
        _resultSource = context.MethodDef!.NoWait
            ? (TaskCompletionSource<TResult>)(object)RpcNoWait.TaskSources.Completed
            : TaskCompletionSourceExt.New<TResult>();
        ResultTask = _resultSource.Task;
    }

    public override bool TryCompleteWithOk(object? result, RpcInboundContext context)
    {
        try {
            if (!_resultSource.TrySetResult((TResult)result!))
                return false;

            Context.Peer!.Calls.Outbound.TryRemove(Id, this);
            return true;
        }
        catch (Exception e) {
            return TryCompleteWithError(e, context);
        }
    }

    public override bool TryCompleteWithError(Exception error, RpcInboundContext? context)
    {
        if (!_resultSource.TrySetException(error))
            return false;

        Context.Peer!.Calls.Outbound.TryRemove(Id, this);
        return true;
    }

    public override bool TryCompleteWithCancel(CancellationToken cancellationToken, RpcInboundContext? context)
    {
        if (!_resultSource.TrySetCanceled(cancellationToken))
            return false;

        Context.Peer!.Calls.Outbound.TryRemove(Id, this);
        return true;
    }
}
