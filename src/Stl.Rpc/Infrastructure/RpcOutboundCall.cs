using Stl.Interception;
using Stl.Internal;

namespace Stl.Rpc.Infrastructure;

public interface IRpcOutboundCall : IRpcCall
{
    RpcOutboundContext Context { get; }
    long Id { get; }
    Task ResultTask { get; }
}

public interface IRpcOutboundCall<TResult> : IRpcOutboundCall
{
    void SetResult(Result<TResult> result, CancellationToken cancellationToken = default);
}

public class RpcOutboundCall<TResult> : RpcCall<TResult>, IRpcOutboundCall<TResult>
{
    private readonly TaskCompletionSource<TResult> _resultSource = TaskCompletionSourceExt.New<TResult>();

    public RpcOutboundContext Context { get; }
    public long Id { get; protected set; }
    public Task ResultTask => _resultSource.Task;

    public RpcOutboundCall(RpcOutboundContext context) : base(context.MethodDef!)
        => Context = context;

    public override Task Start()
    {
        if (Id != 0)
            throw Errors.AlreadyInvoked(nameof(Start));

        var peer = Context.Peer!;
        Id = peer.Calls.NextId;
        var message = CreateCallMessage();

        peer.Calls.Outbound.TryAdd(Id, this);
        var cancellationToken = Context.CancellationToken;
        var sendTask = peer.Send(message, cancellationToken);
        return sendTask.IsCompletedSuccessfully ? Task.CompletedTask : sendTask.AsTask();
    }

    public void SetResult(Result<TResult> result, CancellationToken cancellationToken = default)
        => _resultSource.SetFromResult(result, cancellationToken);

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
        return new RpcMessage(ServiceDef.Name, MethodDef.Name, serializedArguments, headers, Id);
    }
}
