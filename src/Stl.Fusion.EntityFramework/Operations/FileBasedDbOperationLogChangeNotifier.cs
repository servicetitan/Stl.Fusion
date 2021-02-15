using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Stl.CommandR;
using Stl.Fusion.Operations;

namespace Stl.Fusion.EntityFramework.Operations
{
    public class FileBasedDbOperationLogChangeNotifier<TDbContext> : IOperationCompletionListener
        where TDbContext : DbContext
    {
        protected FileBasedDbOperationLogChangeTrackingOptions<TDbContext> Options { get; }
        protected AgentInfo AgentInfo { get; }

        public FileBasedDbOperationLogChangeNotifier(
            FileBasedDbOperationLogChangeTrackingOptions<TDbContext> options,
            AgentInfo agentInfo)
        {
            Options = options;
            AgentInfo = agentInfo;
        }

        public void OnOperationCompleted(IOperation operation)
        {
            if (operation.AgentId != AgentInfo.Id.Value) // Only local commands require notification
                return;
            var commandContext = CommandContext.Current;
            if (commandContext != null) { // It's a command
                var operationScope = commandContext.Items.TryGet<DbOperationScope<TDbContext>>();
                if (operationScope == null || !operationScope.IsUsed) // But it didn't change anything related to TDbContext
                    return;
            }
            // If it wasn't command, we pessimistically assume it changed something

            var filePath = Options.FilePath;
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, "");
            else
                File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);
        }
    }
}
