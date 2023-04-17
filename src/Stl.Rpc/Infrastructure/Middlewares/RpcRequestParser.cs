using Stl.Interception;

#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public class RpcRequestParser : RpcMiddleware
{
    private RpcServiceRegistry ServiceRegistry { get; }
    private RpcMethodResolver MethodResolver { get; }

    public RpcRequestParser(IServiceProvider services) : base(services)
    {
        ServiceRegistry = services.GetRequiredService<RpcServiceRegistry>();
        MethodResolver = Services.GetRequiredService<RpcMethodResolver>();
    }

    public override Task Invoke(RpcContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var headers = request.Headers ?? Array.Empty<RpcHeader>();
        var options = context.Channel.Options;

        var tService = ServiceRegistry[request.Service];
        if (tService == null) {
            Log.LogWarning("Couldn't resolve service '{Service}'", request.Service);
            return Task.CompletedTask;
        }

        if (!options.ServiceFilter.Invoke(tService)) {
            Log.LogWarning("Service '{Service}' isn't white-listed", request.Service);
            return Task.CompletedTask;
        }

        var method = MethodResolver.Resolve(tService, request.Method);
        if (method == null) {
            Log.LogWarning("Service '{Service}' doesn't have '{Method}' method", request.Service, request.Method);
            return Task.CompletedTask;
        }

        var arguments = ArgumentList.Empty;
        var argumentListType = method.RemoteArgumentListType;
        if (argumentListType.IsGenericType) {
            var actualArgumentListType = argumentListType;
            if (headers.Any(static h => h.Name.StartsWith(RpcHeader.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))) {
                var gParameters = argumentListType.GetGenericArguments();
                foreach (var h in headers) {
                    if (!h.Name.StartsWith(RpcHeader.ArgumentTypeHeaderPrefix, StringComparison.Ordinal))
                        continue;
#if NET7_0_OR_GREATER
                    if (!int.TryParse(h.Name.AsSpan(RpcHeader.ArgumentTypeHeaderPrefix.Length), CultureInfo.InvariantCulture, out var index))
#else
#pragma warning disable MA0011
                    if (!int.TryParse(h.Name.Substring(RpcHeader.ArgumentTypeHeaderPrefix.Length), out var index))
#pragma warning restore MA0011
#endif
                        continue;

                    var argumentType = new TypeRef(h.Value).Resolve();
                    if (!gParameters[index].IsAssignableFrom(argumentType)) {
                        Log.LogWarning("Argument #{Index} for '{Service}.{Method}' has incompatible type: '{Type}'",
                            index, request.Service, request.Method, argumentType.GetName());
                        return Task.CompletedTask;
                    }
                    gParameters[index] = argumentType;
                }
                actualArgumentListType = argumentListType
                    .GetGenericTypeDefinition()
                    .MakeGenericType(gParameters);
            }

            var deserializedArguments = options.Deserializer.Invoke(request.Arguments, actualArgumentListType) as ArgumentList;
            if (deserializedArguments == null) {
                Log.LogWarning("Couldn't deserialize arguments for '{Service}.{Method}'", request.Service, request.Method);
                return Task.CompletedTask;
            }

            if (argumentListType == actualArgumentListType)
                arguments = deserializedArguments;
            else {
                arguments = (ArgumentList)argumentListType.CreateInstance();
                arguments.SetFrom(deserializedArguments);
            }
        }

        var parsedRequest = new ParsedRpcRequest(tService, method.Method, arguments, headers);
        context.ParsedRequest = parsedRequest;
        return context.InvokeNextMiddleware(cancellationToken);
    }
}
