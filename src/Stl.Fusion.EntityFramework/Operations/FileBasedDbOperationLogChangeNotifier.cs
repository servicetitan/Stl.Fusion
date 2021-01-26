using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Stl.CommandR;
using Stl.Fusion.Operations;
using Stl.IO;

namespace Stl.Fusion.EntityFramework.Operations
{
    public class FileBasedDbOperationLogChangeNotifier<TDbContext> : IOperationCompletionListener
        where TDbContext : DbContext
    {
        public class Options
        {
            public PathString FilePath { get; set; } = PathString.Empty;
        }

        public PathString FilePath { get; }
        protected AgentInfo AgentInfo { get; }

        public FileBasedDbOperationLogChangeNotifier(Options options, AgentInfo agentInfo)
        {
            FilePath = options.FilePath;
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

            if (!File.Exists(FilePath))
                File.WriteAllText(FilePath, "");
            else
                File.SetLastWriteTimeUtc(FilePath, DateTime.UtcNow);
        }
    }
}
