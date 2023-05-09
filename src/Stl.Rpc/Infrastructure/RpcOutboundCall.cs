using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public interface IRpcOutboundCall : IRpcCall
{
    RpcOutboundContext Context { get; }
    Task ResultTask { get; }
}

public class RpcOutboundCall<T> : RpcCall<T>, IRpcOutboundCall
{
    protected TaskCompletionSource<T> ResultSource { get; } = TaskCompletionSourceExt.New<T>();

    public RpcOutboundContext Context { get; }
    public Task ResultTask => ResultSource.Task;

    public RpcOutboundCall(RpcOutboundContext context) : base(context.MethodDef!)
        => Context = context;

    public override Task Start()
    {
        return Task.CompletedTask;
    }

    // Protected methods

    protected virtual RpcMessage CreateCallMessage()
    {
        var headers = Context.Headers;
        var peer = Context.Peer!;
        var arguments = Context.Arguments!;
        if (MethodDef.CancellationTokenIndex >= 0)
            arguments = arguments.Remove(MethodDef.CancellationTokenIndex);

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
}
