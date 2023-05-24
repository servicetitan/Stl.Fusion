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

    public static RpcInboundCall New(RpcInboundContext context, RpcMethodDef methodDef)
        => FactoryCache.GetOrAdd((context.CallType, methodDef.UnwrappedReturnType), static key => {
            var (tGeneric, tResult) = key;
            var tInbound = tGeneric.MakeGenericType(tResult);
            return (Func<RpcInboundContext, RpcMethodDef, RpcInboundCall>)tInbound
                .GetConstructorDelegate(typeof(RpcInboundContext), typeof(RpcMethodDef))!;
        }).Invoke(context, methodDef);

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

    public abstract Task Process();

    public abstract ValueTask Complete();

    public virtual void Cancel()
    {
        var cts = CancellationTokenSource;
        if (cts != null) {
            CancellationTokenSource = null;
            cts.CancelAndDisposeSilently();
        }
    }

    // Protected methods

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
        if (NoWait)
            return;

        Context.Peer.Calls.Inbound.TryRemove(Id, this);
    }
}

public class RpcInboundCall<TResult> : RpcInboundCall
{
    public Result<TResult> Result { get; protected set; }

    public RpcInboundCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    { }

    public override async Task Process()
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
        var cts = CancellationTokenSource;
        if (cts == null) // NoWait or already completed
            return ValueTaskExt.CompletedTask;

        CancellationTokenSource = null;
        cts.Dispose();
        Unregister();

        if (CancellationToken.IsCancellationRequested) {
            // Call is cancelled @ the outbound end or Peer is disposed, so there is nothing else to do
            return ValueTaskExt.CompletedTask;
        }

        var systemCallSender = Hub.SystemCallSender;
        return systemCallSender.Complete(Context.Peer, Id, Result, ResultHeaders);
    }

    // Protected methods

    protected Task<TResult> InvokeService()
    {
        var methodDef = MethodDef;
        var services = Hub.Services;
        var service = services.GetRequiredService(methodDef.Service.ServerType);
        return (Task<TResult>)methodDef.Invoker.Invoke(service, Arguments!);
    }

    protected ArgumentList DeserializeArguments()
    {
        var peer = Context.Peer;
        var message = Context.Message;
        var isSystemServiceCall = ServiceDef.IsSystem;

        if (!isSystemServiceCall && !peer.LocalServiceFilter.Invoke(ServiceDef))
            throw Errors.ServiceIsNotWhiteListed(ServiceDef);

        var arguments = ArgumentList.Empty;
        var argumentListType = MethodDef.RemoteArgumentListType;
        if (MethodDef.HasObjectTypedArguments) {
            var argumentListTypeResolver = (IRpcArgumentListTypeResolver)Hub.Services
                .GetRequiredService(ServiceDef.ServerType);
            argumentListType = argumentListTypeResolver.GetArgumentListType(Context) ?? argumentListType;
        }

        if (argumentListType.IsGenericType) { // == Has 1+ arguments
            var headers = Context.Headers;
            if (headers.Any(static h => h.Name.StartsWith(RpcHeader.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))) {
                var argumentTypes = argumentListType.GetGenericArguments();
                foreach (var h in headers) {
                    if (!h.Name.StartsWith(RpcHeader.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))
                        continue;
#if NET7_0_OR_GREATER
                    if (!int.TryParse(h.Name.AsSpan(RpcHeader.ArgumentTypeHeaderPrefix.Length), CultureInfo.InvariantCulture, out var argumentIndex))
#else
#pragma warning disable MA0011
                    if (!int.TryParse(h.Name.Substring(RpcHeader.ArgumentTypeHeaderPrefix.Length), out var argumentIndex))
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

            var deserializedArguments = peer.ArgumentSerializer.Deserialize(message, argumentListType);
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
