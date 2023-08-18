namespace Stl.Rpc;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial struct RpcNoWait
{
    public static class Tasks
    {
        public static readonly Task<RpcNoWait> Completed = Task.FromResult(default(RpcNoWait));
    }

    public static class ValueTasks
    {
        public static readonly ValueTask<RpcNoWait> Completed = ValueTaskExt.FromResult(default(RpcNoWait));
    }

    public static class TaskSources
    {
        public static readonly TaskCompletionSource<RpcNoWait> Completed =
            new TaskCompletionSource<RpcNoWait>().WithResult(default);
    }
}
