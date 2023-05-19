namespace Stl.Rpc;

public readonly struct RpcNoWait
{
    public static class Tasks
    {
        public static readonly Task<RpcNoWait> Completed = Task.FromResult(default(RpcNoWait));
    }

    public static class ValueTasks
    {
        public static readonly ValueTask<RpcNoWait> Completed = ValueTaskExt.FromResult(default(RpcNoWait));
    }
}
