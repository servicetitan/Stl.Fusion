namespace Stl.Fusion.Operations.Internal;

public class CompletionProducer : IOperationCompletionListener
{
    public record Options
    {
        public bool IgnoreNotLogged { get; init; } = false;
        public LogLevel LogLevel { get; init; } = LogLevel.Information;
    }

    private AgentInfo? _agentInfo;
    private ILogger? _log;

    protected Options Settings { get; }
    protected ICommander Commander { get; }
    protected IServiceProvider Services => Commander.Services;
    protected AgentInfo AgentInfo => _agentInfo ??= Services.GetRequiredService<AgentInfo>();
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public CompletionProducer(Options settings, ICommander commander, AgentInfo agentInfo)
    {
        Settings = settings;
        Commander = commander;
    }

    public bool IsReady()
        => true;

    public virtual Task OnOperationCompleted(IOperation operation, CommandContext? commandContext)
    {
        if (operation.Command is not ICommand command)
            return Task.CompletedTask; // We can't complete non-commands
        return Task.Run(async () => {
            var isLocal = commandContext != null;
            var operationType = isLocal ? "Local" : "External";
            try {
                // if (command is IBackendCommand backendCommand)
                //     backendCommand.MarkValid();
                await Commander.Call(Completion.New(operation), true).ConfigureAwait(false);
                if (command is not INotLogged || Settings.IgnoreNotLogged)
                    Log.IfEnabled(Settings.LogLevel)?.Log(Settings.LogLevel,
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
