using Stl.Interception;
using Stl.Rpc.Internal;

#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public interface IRpcInboundCall : IRpcCall
{
    RpcInboundContext Context { get; }
    long Id { get; }
    CancellationTokenSource? CancellationTokenSource { get; }
    CancellationToken CancellationToken { get; }

    Task Process(CancellationToken cancellationToken);
}

public class RpcInboundCall<TResult> : RpcCall<TResult>, IRpcInboundCall
{
    public RpcInboundContext Context { get; }
    public long Id { get; }
    public CancellationTokenSource? CancellationTokenSource { get; protected set; }
    public CancellationToken CancellationToken { get; protected set; }

    public RpcInboundCall(RpcInboundContext context) : base(context.MethodDef)
    {
        Context = context;
        Id =  MethodDef.NoWait ? 0 : Context.Message.CallId;
    }

    public virtual async Task Process(CancellationToken cancellationToken)
    {
        Result<TResult> result;
        if (Id != 0) {
            if (CancellationTokenSource != null)
                throw Stl.Internal.Errors.AlreadyInvoked(nameof(Process));

            CancellationTokenSource = cancellationToken.CreateLinkedTokenSource();
            CancellationToken = CancellationTokenSource.Token;
            if (!Context.Peer.Calls.Inbound.TryAdd(Id, this)) {
                var log = Hub.Services.LogFor(GetType());
                log.LogWarning("Inbound {MethodDef} call with duplicate Id = {Id}", MethodDef, Id);
                CancellationTokenSource.CancelAndDisposeSilently();
                return;
            }
        }
        else
            CancellationToken = cancellationToken;

        // NOTE(AY):
        // - CancellationToken below is a token associated with the call itself,
        //   which can be cancelled by the remote caller
        // - and cancellationToken is the token associated with call processing,
        //   which can be cancelled if peer dies.

        try {
            var arguments = Context.Arguments = GetArguments();
            var ctIndex = MethodDef.CancellationTokenIndex;
            if (ctIndex >= 0)
                arguments.SetCancellationToken(ctIndex, CancellationToken);

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
        CancellationTokenSource?.Dispose();
        await Hub.SystemCallSender.Complete(Context.Peer, Id, result).ConfigureAwait(false);
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
