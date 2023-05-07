using Stl.Interception;
using Stl.Rpc.Internal;

#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public class RpcRequestBinder : RpcServiceBase
{
    private RpcServiceRegistry ServiceRegistry { get; }

    public RpcRequestBinder(IServiceProvider services) : base(services)
        => ServiceRegistry = services.GetRequiredService<RpcServiceRegistry>();

    public virtual RpcMessage FromBound(RpcPeer peer, RpcBoundRequest boundRequest)
    {
        var methodDef = boundRequest.MethodDef;
        peer.Hub.OutboundCalls.Register(boundRequest);

        var arguments = boundRequest.Arguments;
        if (methodDef.CancellationTokenIndex >= 0)
            arguments = arguments.Remove(methodDef.CancellationTokenIndex);

        var headers = boundRequest.Headers;
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

        var serializedArguments = peer.ArgumentSerializer.Invoke(arguments, arguments.GetType());
        return new RpcMessage(methodDef.Service.Name, methodDef.Name, serializedArguments, headers);
    }

    public virtual RpcBoundRequest ToBound(RpcPeer peer, RpcMessage message)
    {
        var headers = message.Headers;

        var serviceDef = ServiceRegistry[message.Service];
        if (!serviceDef.IsSystem && !peer.LocalServiceFilter.Invoke(serviceDef))
            throw Errors.ServiceIsNotWhiteListed(serviceDef);

        var methodDef = serviceDef[message.Method];
        var arguments = ArgumentList.Empty;
        var argumentListType = methodDef.RemoteArgumentListType;
        if (argumentListType.IsGenericType) {
            var actualArgumentListType = argumentListType;
            Type[] argumentTypes;
            if (headers != null && headers.Any(static h => h.Name.StartsWith(RpcHeader.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))) {
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
                        throw Errors.IncompatibleArgumentType(methodDef, argumentIndex, argumentType);

                    argumentTypes[argumentIndex] = argumentType;
                }

                actualArgumentListType = argumentListType
                    .GetGenericTypeDefinition()
                    .MakeGenericType(argumentTypes);
            }
            else
                argumentTypes = methodDef.RemoteParameterTypes;

            if (methodDef.MustCheckArguments)
                methodDef.CheckArguments(peer, message, argumentTypes);
            var deserializedArguments = peer.ArgumentDeserializer.Invoke(message.Arguments, actualArgumentListType);
            if (deserializedArguments == null)
                throw Errors.NonDeserializableArguments(methodDef);

            if (argumentListType == actualArgumentListType)
                arguments = deserializedArguments;
            else {
                arguments = (ArgumentList)argumentListType.CreateInstance();
                arguments.SetFrom(deserializedArguments);
            }
        }

        var boundRequest = methodDef.BoundRequestFactory.Invoke(arguments);
        if (headers != null)
            boundRequest.Headers.AddRange(headers);
        return boundRequest;
    }
}
