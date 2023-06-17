using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public abstract class RpcOutboundCall : RpcCall
{
    private static readonly ConcurrentDictionary<(byte, Type), Func<RpcOutboundContext, RpcOutboundCall>> FactoryCache = new();

    public RpcOutboundContext Context { get; }
    public Task ResultTask { get; protected init; } = null!;

    public static RpcOutboundCall New(RpcOutboundContext context)
        => FactoryCache.GetOrAdd((context.CallTypeId, context.MethodDef!.UnwrappedReturnType), static key => {
            var (callTypeId, tResult) = key;
            var type = RpcCallTypeRegistry.Resolve(callTypeId)
                .OutboundCallType
                .MakeGenericType(tResult);
            return (Func<RpcOutboundContext, RpcOutboundCall>)type.GetConstructorDelegate(typeof(RpcOutboundContext))!;
        }).Invoke(context);

    protected RpcOutboundCall(RpcOutboundContext context)
        : base(context.MethodDef!)
        => Context = context;

    public async ValueTask Send()
    {
        RpcMessage message;
        var peer = Context.Peer!;
        if (Context.MethodDef!.NoWait)
            message = CreateMessage(Context.RelatedCallId);
        else if (Id == 0) {
            Id = peer.OutboundCalls.NextId;
            message = CreateMessage(Id);
            peer.OutboundCalls.Register(this);
        }
        else
            message = CreateMessage(Id);
        await peer.Send(message).ConfigureAwait(false);
    }

    public virtual RpcMessage CreateMessage(long callId)
    {
        var headers = Context.Headers;
        var peer = Context.Peer!;
        var arguments = Context.Arguments!;
        var methodDef = MethodDef;
        if (methodDef.CancellationTokenIndex >= 0)
            arguments = arguments.Remove(methodDef.CancellationTokenIndex);

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
                    var h = RpcSystemHeaders.ArgumentTypes[i].With(typeRef.AssemblyQualifiedName);
                    headers = headers.TryAdd(h);
                }
                argumentListType = argumentListType
                    .GetGenericTypeDefinition()
                    .MakeGenericType(gParameters);
                var oldArguments = arguments;
                arguments = (ArgumentList)argumentListType.CreateInstance();
                arguments.SetFrom(oldArguments);
            }
        }
        var argumentData = peer.ArgumentSerializer.Serialize(arguments);
        var message = new RpcMessage(Context.CallTypeId, callId, methodDef.Service.Name, methodDef.Name, argumentData, headers);
        return message;
    }

    public abstract bool TryCompleteWithOk(object? result, RpcInboundContext context);
    public abstract bool TryCompleteWithError(Exception error, RpcInboundContext? context);
    public abstract bool TryCompleteWithCancel(CancellationToken cancellationToken, RpcInboundContext? context);
}

public class RpcOutboundCall<TResult> : RpcOutboundCall
{
    protected readonly TaskCompletionSource<TResult> ResultSource;

    public RpcOutboundCall(RpcOutboundContext context)
        : base(context)
    {
        ResultSource = context.MethodDef!.NoWait
            ? (TaskCompletionSource<TResult>)(object)RpcNoWait.TaskSources.Completed
            : new TaskCompletionSource<TResult>();
        ResultTask = ResultSource.Task;
    }

    public override bool TryCompleteWithOk(object? result, RpcInboundContext context)
    {
        Context.Peer!.OutboundCalls.Unregister(this);
        return ResultSource.TrySetResult((TResult)result!);
    }

    public override bool TryCompleteWithError(Exception error, RpcInboundContext? context)
    {
        Context.Peer!.OutboundCalls.Unregister(this);
        return ResultSource.TrySetException(error);
    }

    public override bool TryCompleteWithCancel(CancellationToken cancellationToken, RpcInboundContext? context)
    {
        Context.Peer!.OutboundCalls.Unregister(this);
        return ResultSource.TrySetCanceled(cancellationToken);
    }
}
