using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;
using Stl.CommandR.Commands;

namespace Stl.Fusion.Operations.Internal
{
    public class CompletionProducer : IOperationCompletionListener
    {
        public class Options
        {
            public LogLevel LogLevel { get; set; } = LogLevel.None;
        }

        protected ICommander Commander { get; }
        protected AgentInfo AgentInfo { get; }
        protected LogLevel LogLevel { get; }
        protected ILogger Log { get; }

        public CompletionProducer(Options? options,
            ICommander commander,
            AgentInfo agentInfo,
            ILogger<CompletionProducer>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<CompletionProducer>.Instance;
            LogLevel = options.LogLevel;
            AgentInfo = agentInfo;
            Commander = commander;
        }

        public virtual void OnOperationCompleted(IOperation operation)
        {
            if (operation.AgentId == AgentInfo.Id.Value)
                return; // Local completions are handled by LocalCompletionProducer
            if (!(operation.Command is ICommand command))
                return; // We can't complete non-commands
            if (command is IServerSideCommand serverSideCommand)
                serverSideCommand.MarkServerSide(); // Server-side commands should be marked as such

            var logEnabled = LogLevel != LogLevel.None && Log.IsEnabled(LogLevel);
            if (logEnabled)
                Log.Log(LogLevel, "External operation completed. Agent: '{AgentId}', Command: {Command}",
                    operation.AgentId, command);

            Commander.Start(Completion.New(operation), true);
        }
    }
}
