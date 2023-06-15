using Stl.Interception;
using Stl.Rpc.Internal;

#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public abstract class RpcInboundCall : RpcCall
{
    private static readonly ConcurrentDictionary<(Type, Type), Func<RpcInboundContext, RpcMethodDef, RpcInboundCall>> FactoryCache = new();

    protected CancellationTokenSource? CancellationTokenSource { get; set; }

    public RpcInboundContext Context { get; }
    public long Id { get; }
    public CancellationToken CancellationToken { get; }
    public ArgumentList? Arguments { get; protected set; } = null;
    public bool NoWait => Id == 0;
    public List<RpcHeader> ResultHeaders { get; } = new();

    public static RpcInboundCall New(Type? callType, RpcInboundContext context, RpcMethodDef? methodDef)
    {
        if (methodDef == null || callType == null) {
            var systemCallsServiceDef = context.Peer.Hub.ServiceRegistry[typeof(IRpcSystemCalls)];
            var notFoundMethodDef = systemCallsServiceDef[nameof(IRpcSystemCalls.NotFound)];
            return new RpcInbound404Call<Unit>(context, notFoundMethodDef);
        }

        return FactoryCache.GetOrAdd((callType, methodDef.UnwrappedReturnType), static key => {
            var (tGeneric, tResult) = key;
            var tInbound = tGeneric.MakeGenericType(tResult);
            return (Func<RpcInboundContext, RpcMethodDef, RpcInboundCall>)tInbound
                .GetConstructorDelegate(typeof(RpcInboundContext), typeof(RpcMethodDef))!;
        }).Invoke(context, methodDef);
    }

    protected RpcInboundCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(methodDef)
    {
        Context = context;
        Id =  methodDef.NoWait ? 0 : context.Message.CallId;
        var cancellationToken = context.CancellationToken;

        if (NoWait)
            CancellationToken = cancellationToken;
        else {
            CancellationTokenSource = cancellationToken.CreateLinkedTokenSource();
            CancellationToken = CancellationTokenSource.Token;
        }
    }

    public abstract Task Invoke();

    public abstract ValueTask Complete();

    public void Cancel()
        => TryComplete(true);

    // Protected methods

    protected bool TryComplete(bool mustCancel)
    {
        var cts = CancellationTokenSource;
        if (cts == null) // NoWait, already completed, or cancelled
            return false;

        CancellationTokenSource = null;
        if (mustCancel)
            cts.CancelAndDisposeSilently();
        else
            cts.Dispose();
        Unregister();
        return true;
    }

    protected bool TryRegister()
    {
        // NoWait should always return true here!
        if (NoWait || Context.Peer.Calls.Inbound.TryAdd(Id, this))
            return true;

        var log = Hub.Services.LogFor(GetType());
        log.LogWarning("Inbound {MethodDef} call with duplicate Id = {Id}", MethodDef, Id);
        CancellationTokenSource.CancelAndDisposeSilently();
        CancellationTokenSource = null;
        return false;
    }

    protected void Unregister()
    {
        if (!NoWait)
            Context.Peer.Calls.Inbound.TryRemove(Id, this);
    }
}

public class RpcInboundCall<TResult> : RpcInboundCall
{
    public Result<TResult> Result { get; protected set; }

    public RpcInboundCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    { }

    public override async Task Invoke()
    {
        if (!TryRegister())
            return;

        try {
            Arguments = DeserializeArguments();
            Result = await InvokeService().ConfigureAwait(false);
        }
        catch (Exception error) {
            Result = new Result<TResult>(default!, error);
        }
        await Complete().ConfigureAwait(false);
    }

    public override ValueTask Complete()
    {
        if (!TryComplete(false))
            return default;

        if (CancellationToken.IsCancellationRequested) {
            // Call is cancelled @ the outbound end or Peer is disposed
            return default;
        }

        var systemCallSender = Hub.SystemCallSender;
        return systemCallSender.Complete(Context.Peer, Id, Result, ResultHeaders);
    }

    // Protected methods

    protected Task<TResult> InvokeService()
    {
        var methodDef = MethodDef;
        var server = methodDef.Service.Server;
        return (Task<TResult>)methodDef.Invoker.Invoke(server, Arguments!);
    }

    protected ArgumentList DeserializeArguments()
    {
        var peer = Context.Peer;
        var message = Context.Message;
        var isSystemServiceCall = ServiceDef.IsSystem;

        if (!isSystemServiceCall && !peer.LocalServiceFilter.Invoke(ServiceDef))
            throw Errors.NoService(ServiceDef.Type);

        var arguments = ArgumentList.Empty;
        var argumentListType = MethodDef.RemoteArgumentListType;
        if (MethodDef.HasObjectTypedArguments) {
            var argumentListTypeResolver = (IRpcArgumentListTypeResolver)ServiceDef.Server;
            argumentListType = argumentListTypeResolver.GetArgumentListType(Context) ?? argumentListType;
        }

        if (argumentListType.IsGenericType) { // == Has 1+ arguments
            var headers = Context.Headers;
            if (headers.Any(static h => h.Name.StartsWith(RpcSystemHeaders.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))) {
                var argumentTypes = argumentListType.GetGenericArguments();
                foreach (var h in headers) {
                    if (!h.Name.StartsWith(RpcSystemHeaders.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))
                        continue;
#if NET7_0_OR_GREATER
                    if (!int.TryParse(
                        h.Name.AsSpan(RpcSystemHeaders.ArgumentTypeHeaderPrefix.Length),
                        CultureInfo.InvariantCulture,
                        out var argumentIndex))
#else
#pragma warning disable MA0011
                    if (!int.TryParse(
                        h.Name.Substring(RpcSystemHeaders.ArgumentTypeHeaderPrefix.Length),
                        out var argumentIndex))
#pragma warning restore MA0011
#endif
                        continue;

                    var argumentType = new TypeRef(h.Value).Resolve();
                    if (!argumentTypes[argumentIndex].IsAssignableFrom(argumentType))
                        throw Errors.IncompatibleArgumentType(MethodDef, argumentIndex, argumentType);

                    argumentTypes[argumentIndex] = argumentType;
                }
                argumentListType = argumentListType
                    .GetGenericTypeDefinition()
                    .MakeGenericType(argumentTypes);
            }

            var deserializedArguments = peer.ArgumentSerializer.Deserialize(message.ArgumentData, argumentListType);
            if (argumentListType == MethodDef.ArgumentListType)
                arguments = deserializedArguments;
            else {
                arguments = (ArgumentList)MethodDef.ArgumentListType.CreateInstance();
                var ctIndex = MethodDef.CancellationTokenIndex;
                if (ctIndex >= 0)
                    deserializedArguments = deserializedArguments.InsertCancellationToken(ctIndex, CancellationToken);
                arguments.SetFrom(deserializedArguments);
            }
        }

        return arguments;
    }
}
