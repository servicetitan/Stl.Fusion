using Stl.Interception;
using Stl.Internal;

namespace Stl.Rpc.Infrastructure;

public interface IRpcOutboundCall : IRpcCall
{
    RpcOutboundContext Context { get; }
    long Id { get; }
    Task ResultTask { get; }

    Task Send();
    void Complete(object? result);
    void Complete(ExceptionInfo error);
}

public interface IRpcOutboundCall<TResult> : IRpcOutboundCall
{ }

public class RpcOutboundCall<TResult> : RpcCall<TResult>, IRpcOutboundCall<TResult>
{
    private readonly TaskCompletionSource<TResult> _resultSource = TaskCompletionSourceExt.New<TResult>();

    public RpcOutboundContext Context { get; }
    public long Id { get; protected set; }
    public Task ResultTask => _resultSource.Task;

    public RpcOutboundCall(RpcOutboundContext context) : base(context.MethodDef!)
        => Context = context;

    public virtual Task Send()
    {
        if (Id != 0)
            throw Errors.AlreadyInvoked(nameof(Send));

        var peer = Context.Peer!;
        var noWait = Context.MethodDef!.NoWait;
        Id = noWait ? Context.RelatedCallId : peer.Calls.NextId;
        var message = CreateCallMessage();

        if (!noWait)
            peer.Calls.Outbound.TryAdd(Id, this);
        var cancellationToken = Context.CancellationToken;
        var sendTask = peer.Send(message, cancellationToken);
        return sendTask.IsCompletedSuccessfully ? Task.CompletedTask : sendTask.AsTask();
    }

    public virtual void Complete(object? result)
    {
        var peer = Context.Peer!;
        if (peer.Calls.Outbound.TryRemove(Id, this))
            _resultSource.SetResult((TResult)result!);
    }

    public virtual void Complete(ExceptionInfo error)
    {
        var peer = Context.Peer!;
        if (peer.Calls.Outbound.TryRemove(Id, this))
            _resultSource.SetException(error.ToException()!);
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
        return new RpcMessage(ServiceDef.Name, MethodDef.Name, serializedArguments, headers, Id);
    }
}
