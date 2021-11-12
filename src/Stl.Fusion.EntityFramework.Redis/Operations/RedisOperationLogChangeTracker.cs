using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.EntityFramework.Operations;
using Stl.Redis;

namespace Stl.Fusion.EntityFramework.Redis.Operations;

public class RedisOperationLogChangeTracker<TDbContext> : DbWakeSleepProcessBase<TDbContext>,
    IDbOperationLogChangeTracker<TDbContext>
    where TDbContext : DbContext
{
    public RedisOperationLogChangeTrackingOptions<TDbContext> Options { get; }
    protected AgentInfo AgentInfo { get; }
    protected Task<Unit> NextEventTask { get; set; } = null!;
    protected RedisDb RedisDb { get; }
    protected RedisPubSub RedisPubSub { get; }
    protected object Lock { get; } = new();

    public RedisOperationLogChangeTracker(
        RedisOperationLogChangeTrackingOptions<TDbContext> options,
        AgentInfo agentInfo,
        IServiceProvider services)
        : base(services)
    {
        Options = options;
        AgentInfo = agentInfo;
        RedisDb = Services.GetService<RedisDb<TDbContext>>() ?? Services.GetRequiredService<RedisDb>();
        RedisPubSub = RedisDb.GetPubSub(options.PubSubKey);
        Log.LogInformation("Using pub/sub key = '{Key}'", RedisPubSub.FullKey);
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

    protected override async Task WakeUp(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested) {
            var value = (string) await RedisPubSub.Read(cancellationToken).ConfigureAwait(false);
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
