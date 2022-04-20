using Microsoft.EntityFrameworkCore;
using Stl.Redis;

namespace Stl.Fusion.EntityFramework.Redis.Operations;

public class RedisOperationLogChangeNotifier<TDbContext> : DbServiceBase<TDbContext>, IOperationCompletionListener
    where TDbContext : DbContext
{
    public RedisOperationLogChangeTrackingOptions<TDbContext> Options { get; }

    protected AgentInfo AgentInfo { get; }
    protected RedisDb RedisDb { get; }
    protected RedisPub RedisPub { get; }

    public RedisOperationLogChangeNotifier(
        RedisOperationLogChangeTrackingOptions<TDbContext> options,
        IServiceProvider services)
        : base(services)
    {
        Options = options;
        AgentInfo = services.GetRequiredService<AgentInfo>();
        RedisDb = services.GetService<RedisDb<TDbContext>>() ?? services.GetRequiredService<RedisDb>();
        RedisPub = RedisDb.GetPub(options.PubSubKey);
        Log.LogInformation("Using pub/sub key = '{Key}'", RedisPub.FullKey);
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
                await RedisPub.Publish(AgentInfo.Id.Value).ConfigureAwait(false);
                return;
            }
            catch (Exception e) {
                Log.LogError(e, "Failed to publish to pub/sub key = '{Key}'; retrying", RedisPub.FullKey);
                await Clocks.CoarseCpuClock.Delay(Options.RetryDelay).ConfigureAwait(false);
            }
        }
    }
}
