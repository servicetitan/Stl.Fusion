using System.Globalization;
using Stl.Interception;

namespace Stl.Rpc.Infrastructure.Middlewares;

public class RpcRequestParser : RpcMiddleware
{
    private static readonly ConcurrentDictionary<(Type Service, string Method), RpcMethodInfo?> MethodCache = new();

    public RpcRequestParser(IServiceProvider services) : base(services) { }

    public override Task Invoke(RpcContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var headers = request.Headers ?? Array.Empty<RpcHeader>();
        var options = context.Channel.Options;

        var tService = TypeRef.Resolve(request.Service);
        if (tService == null) {
            Log.LogWarning("Couldn't resolve service '{Service}'", request.Service);
            return Task.CompletedTask;
        }

        if (options.Services.Contains(tService)) {
            Log.LogWarning("Service '{Service}' isn't white-listed", request.Service);
            return Task.CompletedTask;
        }

        var method = TryResolveMethod(tService, request.Method);
        if (method == null) {
            Log.LogWarning("Service '{Service}' doesn't have '{Method}' method", request.Service, request.Method);
            return Task.CompletedTask;
        }

        var arguments = ArgumentList.Empty;
        var argumentListType = method.ArgumentListType;
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

                    gParameters[index] = new TypeRef(h.Value).Resolve();
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
        return Task.CompletedTask;
    }

    private static RpcMethodInfo? TryResolveMethod(Type serviceType, string methodName)
        => MethodCache.GetOrAdd((serviceType, methodName), key => {
            var (tService, methodName1) = key;
            switch (methodName1.Split(':')) {
            case [var mName, var sArgumentCount]:
#if NET7_0_OR_GREATER
                if (!int.TryParse(sArgumentCount, CultureInfo.InvariantCulture, out var argumentCount))
#else
#pragma warning disable MA0011
                if (!int.TryParse(sArgumentCount, out var argumentCount))
#pragma warning restore MA0011
#endif
                    return null;

                var candidateMethods = tService.GetMethods(BindingFlags.Instance | BindingFlags.Public);
                var method = candidateMethods
                    .SingleOrDefault(m =>
                        Equals(m.Name, mName)
                        && m.GetParameters().Length == argumentCount
                        && !m.IsGenericMethodDefinition);
                if (method == null)
                    return null;

                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                    return new RpcMethodInfo(method, parameters, typeof(ArgumentList0));

                var argumentListType = ArgumentList
                    .Types[parameters.Length]
                    .MakeGenericType(parameters.Select(p => p.ParameterType).ToArray());
                return new RpcMethodInfo(method, parameters, argumentListType);
            default:
                return null;
            }
        });

    // Nested types

    private sealed record RpcMethodInfo(
        MethodInfo Method,
        ParameterInfo[] Parameters,
        Type ArgumentListType);
}
