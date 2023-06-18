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

    public ValueTask Send()
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
        return peer.Send(message);
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

    public abstract void SetResult(object? result, RpcInboundContext context);
    public abstract void SetError(Exception error, RpcInboundContext? context);
    public abstract bool SetCancelled(CancellationToken cancellationToken, RpcInboundContext? context);

    // Protected methods

    protected bool Unregister()
        => Context.Peer!.OutboundCalls.Unregister(this);
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

    public override void SetResult(object? result, RpcInboundContext context)
    {
        if (ResultSource.TrySetResult((TResult)result!))
            Unregister();
    }

    public override void SetError(Exception error, RpcInboundContext? context)
    {
        if (ResultSource.TrySetException(error))
            Unregister();
    }

    public override bool SetCancelled(CancellationToken cancellationToken, RpcInboundContext? context)
    {
        var isCancelled = ResultSource.TrySetCanceled(cancellationToken);
        if (isCancelled)
            Unregister();
        return isCancelled;
    }
}
