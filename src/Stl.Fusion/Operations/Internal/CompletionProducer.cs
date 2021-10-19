using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Fusion.Operations.Internal;

public class CompletionProducer : IOperationCompletionListener
{
    public class Options
    {
        public bool IsLoggingEnabled { get; set; } = true;
    }

    protected ICommander Commander { get; }
    protected AgentInfo AgentInfo { get; }
    protected ILogger Log { get; }
    protected bool IsLoggingEnabled { get; set; }
    protected LogLevel LogLevel { get; set; } = LogLevel.Information;

    public CompletionProducer(Options? options,
        ICommander commander,
        AgentInfo agentInfo,
        ILogger<CompletionProducer>? log = null)
    {
        options ??= new();
        Log = log ?? NullLogger<CompletionProducer>.Instance;
        IsLoggingEnabled = options.IsLoggingEnabled && Log.IsEnabled(LogLevel);

        AgentInfo = agentInfo;
        Commander = commander;
    }

    public virtual Task OnOperationCompleted(IOperation operation)
    {
        if (!(operation.Command is ICommand command))
            return Task.CompletedTask; // We can't complete non-commands
        return Task.Run(async () => {
            var isLocal = operation.AgentId == AgentInfo.Id.Value;
            var operationType = isLocal ? "Local" : "External";
            try {
                if (command is IServerSideCommand serverSideCommand)
                    serverSideCommand.MarkServerSide(); // Server-side commands should be marked as such
                await Commander.Call(Completion.New(operation), true).ConfigureAwait(false);
                if (IsLoggingEnabled)
                    Log.Log(LogLevel,
                        "{OperationType} operation completion succeeded. Agent: '{AgentId}', Command: {Command}",
                        operationType, operation.AgentId, command);
            }
            catch (Exception e) {
                Log.LogError(e,
                    "{OperationType} operation completion failed! Agent: '{AgentId}', Command: {Command}",
                    operationType, operation.AgentId, command);
            }
        });
    }
}
