using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework.Operations;

public class FileBasedDbOperationLogChangeNotifier<TDbContext> : IOperationCompletionListener
    where TDbContext : DbContext
{
    protected FileBasedDbOperationLogChangeTrackingOptions<TDbContext> Options { get; init; }
    protected AgentInfo AgentInfo { get; init; }

    public FileBasedDbOperationLogChangeNotifier(
        FileBasedDbOperationLogChangeTrackingOptions<TDbContext> options,
        AgentInfo agentInfo)
    {
        Options = options;
        AgentInfo = agentInfo;
    }

    public Task OnOperationCompleted(IOperation operation)
    {
        if (operation.AgentId != AgentInfo.Id.Value) // Only local commands require notification
            return Task.CompletedTask;
        var commandContext = CommandContext.Current;
        if (commandContext != null) { // It's a command
            var operationScope = commandContext.Items.GetOrDefault<DbOperationScope<TDbContext>>();
            if (operationScope == null || !operationScope.IsUsed) // But it didn't change anything related to TDbContext
                return Task.CompletedTask;
        }
        // If it wasn't command, we pessimistically assume it changed something

        var filePath = Options.FilePath;
        if (!File.Exists(filePath))
            File.WriteAllText(filePath, "");
        else
            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
