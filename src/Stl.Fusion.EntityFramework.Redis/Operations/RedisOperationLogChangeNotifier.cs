using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Locking;
using Stl.Redis;

namespace Stl.Fusion.EntityFramework.Redis.Operations;

public class RedisOperationLogChangeNotifier<TDbContext> : DbServiceBase<TDbContext>,
    IOperationCompletionListener
    where TDbContext : DbContext
{
    public RedisOperationLogChangeTrackingOptions<TDbContext> Options { get; }
    protected AgentInfo AgentInfo { get; }
    protected RedisDb RedisDb { get; }
    protected RedisPubSub RedisPubSub { get; }

    public RedisOperationLogChangeNotifier(
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
    }

    public Task OnOperationCompleted(IOperation operation)
    {
        if (!StringComparer.Ordinal.Equals(operation.AgentId, AgentInfo.Id.Value)) // Only local commands require notification
            return Task.CompletedTask;
        var commandContext = CommandContext.Current;
        if (commandContext != null) { // It's a command
            var operationScope = commandContext.Items.Get<DbOperationScope<TDbContext>>();
            if (operationScope == null || !operationScope.IsUsed) // But it didn't change anything related to TDbContext
                return Task.CompletedTask;
        }
        // If it wasn't command, we pessimistically assume it changed something
        using var _ = ExecutionContextExt.SuppressFlow();
        Task.Run(Notify);
        return Task.CompletedTask;
    }

    // Protected methods

    protected virtual async Task Notify()
    {
        while (true) {
            try {
                await RedisPubSub.Publish(AgentInfo.Id.Value).ConfigureAwait(false);
                return;
            }
            catch (Exception e) {
                Log.LogError(e, "Failed to publish to pub/sub key = '{Key}'; retrying", RedisPubSub.FullKey);
                await Clocks.CoarseCpuClock.Delay(Options.RetryDelay).ConfigureAwait(false);
            }
        }
    }
}
