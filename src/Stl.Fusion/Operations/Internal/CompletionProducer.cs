using System;
using System.Threading.Tasks;
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

            Task.Run(async () => {
                try {
                    await Commander.CallAsync(Completion.New(operation), true).ConfigureAwait(false);
                    var logEnabled = LogLevel != LogLevel.None && Log.IsEnabled(LogLevel);
                    if (logEnabled)
                        Log.Log(LogLevel,
                            "External operation completion succeeded. Agent: '{AgentId}', Command: {Command}",
                            operation.AgentId, command);
                }
                catch (Exception e) {
                    Log.LogError(e,
                        "External operation completion failed! Agent: '{AgentId}', Command: {Command}",
                        operation.AgentId, command);
                }
            });
        }
    }
}
