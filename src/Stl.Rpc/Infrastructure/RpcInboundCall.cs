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

        if (Id != 0) {
            CancellationTokenSource = cancellationToken.CreateLinkedTokenSource();
            CancellationToken = CancellationTokenSource.Token;
        }
        else
            CancellationToken = cancellationToken;
    }

    public abstract Task Process();

    public virtual void Complete()
    {
        var cts = CancellationTokenSource;
        CancellationTokenSource = null;
        cts.DisposeSilently();
    }

    public virtual void Cancel()
    {
        var cts = CancellationTokenSource;
        CancellationTokenSource = null;
        cts.CancelAndDisposeSilently();
    }
}

public class RpcInboundCall<TResult> : RpcInboundCall
{
    public RpcInboundCall(RpcInboundContext context, RpcMethodDef methodDef)
        : base(context, methodDef)
    { }

    public override async Task Process()
    {
        if (Id != 0 && !Context.Peer.Calls.Inbound.TryAdd(Id, this)) {
            var log = Hub.Services.LogFor(GetType());
            log.LogWarning("Inbound {MethodDef} call with duplicate Id = {Id}", MethodDef, Id);
            Complete();
            return;
        }

        var cancellationToken = CancellationToken;
        Result<TResult> result;
        try {
            var arguments = Arguments = GetArguments();
            var ctIndex = MethodDef.CancellationTokenIndex;
            if (ctIndex >= 0)
                arguments.SetCancellationToken(ctIndex, cancellationToken);

            var services = Hub.Services;
            var service = services.GetRequiredService(ServiceDef.ServerType);
            var untypedResultTask = MethodDef.Invoker.Invoke(service, arguments);
            await untypedResultTask.ConfigureAwait(false);
            if (MethodDef.IsAsyncVoidMethod)
                result = default;
            else {
                var resultTask = (Task<TResult>)untypedResultTask;
                result = resultTask.ToResultSynchronously();
            }
        }
        catch (Exception error) {
            result = Result.Error<TResult>(error);
        }

        if (Id == 0)
            return; // NoWait call

        Context.Peer.Calls.Inbound.TryRemove(Id, this); // Should always succeed
        Complete();
        if (!cancellationToken.IsCancellationRequested) {
            // If the opposite is true, the call is already cancelled @ the outbound end,
            // so no notification is needed 
            await Hub.SystemCallSender.Complete(Context.Peer, Id, result).ConfigureAwait(false);
        }
    }

    protected ArgumentList GetArguments()
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
                    deserializedArguments = deserializedArguments.InsertCancellationToken(ctIndex, default);
                arguments.SetFrom(deserializedArguments);
            }
        }

        return arguments;
    }
}
