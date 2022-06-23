namespace Stl.CommandR.Internal;

public class Commander : ICommander
{
    protected ILogger Log { get; init; }
    protected ICommandHandlerResolver HandlerResolver { get; }

    public CommanderOptions Options { get; }
    public IServiceProvider Services { get; }

    public Commander(
        CommanderOptions options,
        IServiceProvider services,
        ILogger<Commander>? log = null)
    {
        Log = log ?? NullLogger<Commander>.Instance;
        Options = options;
        Services = services;
        HandlerResolver = services.GetRequiredService<ICommandHandlerResolver>();
    }

    public Task Run(CommandContext context, CancellationToken cancellationToken = default)
    {
        // Task.Run is used to call RunInternal to make sure parent
        // task's ExecutionContext won't be "polluted" by temp.
        // change of CommandContext.Current (via AsyncLocal).
        using var _ = context.IsOutermost ? ExecutionContextExt.SuppressFlow() : default;
        return Task.Run(() => RunInternal(context, cancellationToken), default);
    }

    protected virtual async Task RunInternal(
        CommandContext context, CancellationToken cancellationToken = default)
    {
        try {
            var command = context.UntypedCommand;
            var handlers = HandlerResolver.GetCommandHandlers(command.GetType());
            context.ExecutionState = new CommandExecutionState(handlers);
            if (handlers.Count == 0)
                await OnUnhandledCommand(command, context, cancellationToken).ConfigureAwait(false);

            using var _ = context.Activate();
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) {
            context.SetResult(e);
        }
        finally {
            context.TryComplete(cancellationToken);
            await context.DisposeAsync().ConfigureAwait(false);
        }
    }

    protected virtual Task OnUnhandledCommand(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken)
        => throw Errors.NoHandlerFound(command.GetType());
}
