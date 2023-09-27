namespace Stl.Rpc;

[StructLayout(LayoutKind.Sequential)] // Important!
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public readonly partial struct RpcNoWait
{
    public static class Tasks
    {
        public static readonly Task<RpcNoWait> Completed = Task.FromResult(default(RpcNoWait));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<RpcNoWait> From(Task task)
            => task.IsCompletedSuccessfully() ? Completed : FromAsync(task);

        private static async Task<RpcNoWait> FromAsync(Task task)
        {
            await task.ConfigureAwait(false);
            return default;
        }
    }

    public static class ValueTasks
    {
        public static readonly ValueTask<RpcNoWait> Completed = ValueTaskExt.FromResult(default(RpcNoWait));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<RpcNoWait> From(ValueTask task)
            => task.IsCompletedSuccessfully ? Completed : FromAsync(task);

        private static async ValueTask<RpcNoWait> FromAsync(ValueTask task)
        {
            await task.ConfigureAwait(false);
            return default;
        }
    }

    public static class TaskSources
    {
        public static readonly TaskCompletionSource<RpcNoWait> Completed =
            new TaskCompletionSource<RpcNoWait>().WithResult(default);
    }
}
