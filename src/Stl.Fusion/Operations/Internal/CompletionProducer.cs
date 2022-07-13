namespace Stl.Fusion.Operations.Internal;

public class CompletionProducer : IOperationCompletionListener
{
    public record Options
    {
        public LogLevel LogLevel { get; init; } = LogLevel.Information;
    }

    protected Options Settings { get; }
    protected ICommander Commander { get; }
    protected IServiceProvider Services => Commander.Services; 
    protected AgentInfo AgentInfo { get; }
    protected ILogger Log { get; }
    protected bool IsLoggingEnabled { get; }

    public CompletionProducer(Options settings, ICommander commander, AgentInfo agentInfo)
    {
        Settings = settings;
        Commander = commander;
        AgentInfo = agentInfo;

        Log = Services.LogFor(GetType());
        IsLoggingEnabled = Log.IsLogging(settings.LogLevel);
    }

    public bool IsReady()
        => true;

    public virtual Task OnOperationCompleted(IOperation operation)
    {
        if (operation.Command is not ICommand command)
            return Task.CompletedTask; // We can't complete non-commands
        return Task.Run(async () => {
            var isLocal = StringComparer.Ordinal.Equals(operation.AgentId, AgentInfo.Id.Value);
            var operationType = isLocal ? "Local" : "External";
            try {
                // if (command is IBackendCommand backendCommand)
                //     backendCommand.MarkValid();
                await Commander.Call(Completion.New(operation), true).ConfigureAwait(false);
                if (IsLoggingEnabled)
                    Log.Log(Settings.LogLevel,
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
