using Stl.Interception;
using Stl.Rpc.Internal;

#if NET7_0_OR_GREATER
using System.Globalization;
#endif

namespace Stl.Rpc.Infrastructure;

public abstract class RpcHandler
{
    public RpcHub Hub { get; }
    public RpcMethodDef MethodDef { get; }
    public RpcServiceDef ServiceDef { get; }

    public abstract Task HandleOutbound(RpcOutboundContext context);
    public abstract Task HandleInbound(RpcInboundContext context);

    protected RpcHandler(RpcMethodDef methodDef)
    {
        MethodDef = methodDef;
        ServiceDef = methodDef.Service;
        Hub = ServiceDef.Hub;
    }
}

public class RpcHandler<T> : RpcHandler
{
    public RpcHandler(RpcMethodDef methodDef) : base(methodDef) { }

    public override Task HandleOutbound(RpcOutboundContext context)
    {
        throw new NotSupportedException();
    }

    public override Task HandleInbound(RpcInboundContext context)
    {
        var call = context.Call = CreateCall(context);
        return InvokeCall(call);
    }

    // Protected methods

    protected virtual RpcMessage CreateMessage(RpcOutboundContext context)
    {
        var call = context.Call;
        var peer = context.Peer!;
        var arguments = call.Arguments;
        if (MethodDef.CancellationTokenIndex >= 0)
            arguments = arguments.Remove(MethodDef.CancellationTokenIndex);

        var headers = context.Headers;
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
        return new RpcMessage(ServiceDef.Name, MethodDef.Name, serializedArguments, headers);
    }

    protected RpcCall<T> CreateCall(RpcInboundContext context)
    {
        var peer = context.Peer;
        var message = context.Message;
        var isSystemServiceCall = ServiceDef.IsSystem;

        if (!isSystemServiceCall && !peer.LocalServiceFilter.Invoke(ServiceDef))
            throw Errors.ServiceIsNotWhiteListed(ServiceDef);

        var arguments = ArgumentList.Empty;
        var argumentListType = MethodDef.RemoteArgumentListType;
        if (argumentListType.IsGenericType) {
            var actualArgumentListType = argumentListType;
            Type[] argumentTypes;
            var headers = context.Headers;
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
                ValidateArgumentTypes(context, argumentTypes);
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

        return CreateCall(arguments);
    }

    protected virtual void ValidateArgumentTypes(RpcInboundContext context, Type[] argumentTypes)
    { }

    // CreateCall, InvokeCall

    protected virtual RpcCall<T> CreateCall(ArgumentList arguments)
        => new(MethodDef, arguments);

    protected static Task InvokeCall(RpcCall call)
    {
        var arguments = call.Arguments;
        var methodDef = call.MethodDef;
        var hub = methodDef.Service.Hub;
        var service = hub.Services.GetRequiredService(methodDef.Service.ServerType);
        var result = arguments.GetInvoker(methodDef.Method).Invoke(service, arguments);
        if (methodDef.ReturnsTask) {
            var task = (Task)result!;
            if (methodDef.IsAsyncVoidMethod)
                task = ToUnitTask(task);
            return task;
        }

        if (methodDef.ReturnsValueTask) {
            if (result is ValueTask<T> valueTask)
                return valueTask.AsTask();
            if (result is ValueTask voidValueTask)
                return ToUnitTask(voidValueTask);
        }

        return Task.FromResult((T)result!);
    }

    // Private methods

    private static async Task<Unit> ToUnitTask(Task source)
    {
        await source.ConfigureAwait(false);
        return default;
    }

    private static async Task<Unit> ToUnitTask(ValueTask source)
    {
        await source.ConfigureAwait(false);
        return default;
    }
}
