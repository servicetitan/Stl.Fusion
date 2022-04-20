using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Redis;

namespace Stl.Fusion.EntityFramework.Redis.Operations;

public class RedisOperationLogChangeTracker<TDbContext> : DbWakeSleepWorkerBase<TDbContext>,
    IDbOperationLogChangeTracker<TDbContext>
    where TDbContext : DbContext
{
    public RedisOperationLogChangeTrackingOptions<TDbContext> Options { get; }
    protected AgentInfo AgentInfo { get; }
    protected RedisDb RedisDb { get; }
    protected RedisChannelSub RedisSub { get; }
    protected Task<Unit> NextEventTask { get; set; } = null!;

    public RedisOperationLogChangeTracker(
        RedisOperationLogChangeTrackingOptions<TDbContext> options,
        IServiceProvider services)
        : base(services)
    {
        Options = options;
        AgentInfo = services.GetRequiredService<AgentInfo>();
        RedisDb = services.GetService<RedisDb<TDbContext>>() ?? services.GetRequiredService<RedisDb>();
        RedisSub = RedisDb.GetChannelSub(options.PubSubKey);
        Log.LogInformation("Using pub/sub key = '{Key}'", RedisSub.FullKey);

        // ReSharper disable once VirtualMemberCallInConstructor
        ReplaceNextEventTask();
    }

    public Task WaitForChanges(CancellationToken cancellationToken = default)
    {
        lock (Lock) {
            var task = NextEventTask;
            if (NextEventTask.IsCompleted)
                ReplaceNextEventTask();
            return task;
        }
    }

    // Protected methods

    protected override async Task DisposeAsyncCore()
    {
        await base.DisposeAsyncCore().ConfigureAwait(false);
        await RedisSub.DisposeAsync().ConfigureAwait(false);
    }

    protected override async Task WakeUp(CancellationToken cancellationToken)
    {
        await RedisSub.Subscribe().ConfigureAwait(false);
        while (!cancellationToken.IsCancellationRequested) {
            var value = await RedisSub.Messages
                .ReadAsync(cancellationToken)
                .ConfigureAwait(false);
            if (!StringComparer.Ordinal.Equals(AgentInfo.Id.Value, value))
                ReleaseWaitForChanges();
        }
    }

    protected override Task Sleep(Exception? error, CancellationToken cancellationToken)
        => error != null
            ? Clocks.CoarseCpuClock.Delay(Options.RetryDelay, cancellationToken)
            : Task.CompletedTask;

    protected virtual void ReleaseWaitForChanges()
    {
        lock (Lock)
            TaskSource.For(NextEventTask).TrySetResult(default);
    }

    protected virtual void ReplaceNextEventTask()
        => NextEventTask = TaskSource.New<Unit>(false).Task;
}
