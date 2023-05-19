using Stl.Interception;
using Stl.Rpc.Internal;

#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public interface IRpcInboundCall : IRpcCall
{
    Task Process();
}

public class RpcInboundCall<TResult> : RpcCall<TResult>, IRpcInboundCall
{
    public RpcInboundContext Context { get; }

    public RpcInboundCall(RpcInboundContext context) : base(context.MethodDef) 
        => Context = context;

    public virtual async Task Process()
    {
        var cancellationToken = Context.CancellationToken;
        var result = default(Result<TResult>);
        try {
            var arguments = Context.Arguments = GetArguments();
            var ctIndex = MethodDef.CancellationTokenIndex;
            if (ctIndex >= 0)
                arguments.SetCancellationToken(ctIndex, cancellationToken);

            var services = Hub.Services;
            var service = services.GetRequiredService(ServiceDef.ServerType);
            var untypedResultTask = MethodDef.Invoker.Invoke(service, arguments);
            await untypedResultTask.ConfigureAwait(false);
            if (!MethodDef.IsAsyncVoidMethod) {
                var resultTask = (Task<TResult>)untypedResultTask;
                result = resultTask.ToResultSynchronously();
            }
        }
        catch (Exception error) {
            result = Result.Error<TResult>(error);
        }
        await Send(result, cancellationToken).ConfigureAwait(false);
    }

    protected Task Send(Result<TResult> result, CancellationToken cancellationToken)
    {
        if (Context.MethodDef.NoWait)
            return Task.CompletedTask; // NoWait call

        var callId = Context.Message.CallId;
        if (callId == 0)
            return Task.CompletedTask; // No result implied

        var outboundContext = new RpcOutboundContext();
        using var _ = outboundContext.Activate();
        outboundContext.Peer = Context.Peer;
        outboundContext.RelatedCallId = callId;

        var systemCalls = Hub.Services.GetRequiredService<IRpcSystemCallsClient>();
        return result.IsValue(out var value)
            ? systemCalls.Result(value)
            : systemCalls.Error(result.Error.ToExceptionInfo());
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
        if (argumentListType.IsGenericType) {
            var actualArgumentListType = argumentListType;
            Type[] argumentTypes;
            var headers = Context.Headers;
            if (headers.Any(static h => h.Name.StartsWith(RpcHeader.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))) {
                argumentTypes = argumentListType.GetGenericArguments();
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

                actualArgumentListType = argumentListType
                    .GetGenericTypeDefinition()
                    .MakeGenericType(argumentTypes);
            }
            else
                argumentTypes = MethodDef.RemoteParameterTypes;

            if (MethodDef.RequiresValidation) {
                var callValidator = (IRpcCallValidator)Hub.Services.GetRequiredService(ServiceDef.ServerType);
                callValidator.ValidateCall(Context, argumentTypes);
            }
            var deserializedArguments = peer.ArgumentDeserializer.Invoke(message.Arguments, actualArgumentListType);
            if (deserializedArguments == null)
                throw Errors.NonDeserializableArguments(MethodDef);

            if (argumentListType == actualArgumentListType)
                arguments = deserializedArguments;
            else {
                arguments = (ArgumentList)argumentListType.CreateInstance();
                arguments.SetFrom(deserializedArguments);
            }
        }

        return arguments;
    }
}
