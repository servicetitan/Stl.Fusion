using Stl.Interception;
using Stl.Rpc.Internal;

#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public interface IRpcInboundCall : IRpcCall
{ }

public class RpcInboundCall<T> : RpcCall<T>, IRpcInboundCall
{
    public RpcInboundContext Context { get; }

    public RpcInboundCall(RpcInboundContext context) : base(context.MethodDef) 
        => Context = context;

    public override Task Start()
    {
        var cancellationToken = Context.CancellationToken;
        var arguments = Context.Arguments = GetArguments();
        var ctIndex = MethodDef.CancellationTokenIndex;
        if (ctIndex >= 0)
            arguments.SetCancellationToken(ctIndex, cancellationToken);

        var service = Hub.Services.GetRequiredService(ServiceDef.ServerType);
        var resultTask = MethodDef.Invoker.Invoke(service, arguments);
        // TODO: Publish resultTask result
        return Task.CompletedTask;
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

            if (isSystemServiceCall)
                ValidateArgumentTypes(argumentTypes);
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

    protected virtual void ValidateArgumentTypes(Type[] argumentTypes)
    { }
}
