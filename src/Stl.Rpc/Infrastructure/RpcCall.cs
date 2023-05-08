using Stl.Interception;

namespace Stl.Rpc.Infrastructure;

public abstract record RpcCall(
    RpcMethodDef MethodDef,
    ArgumentList Arguments)
{
    public List<RpcHeader> Headers { get; init; } = new();
    public abstract Task UntypedResultTask { get; }
}

public sealed record RpcCall<T>(
    RpcMethodDef MethodDef,
    ArgumentList Arguments
    ) : RpcCall(MethodDef, Arguments)
{
    public TaskCompletionSource<T> ResultSource { get; } = TaskCompletionSourceExt.New<T>();
    public Task<T> ResultTask => ResultSource.Task;
    public override Task UntypedResultTask => ResultSource.Task;
}
