namespace Stl.CommandR.Internal;

public class Commander(IServiceProvider services) : ICommander
{
    private static readonly PropertyInfo ChainIdSetterProperty =
        typeof(IEventCommand).GetProperty(nameof(IEventCommand.ChainId))!;

    private ILogger? _log;
    private CommandHandlerResolver? _handlerResolver;
    private Action<IEventCommand, Symbol>? _chainIdSetter;

    public IServiceProvider Services { get; } = services;

    protected CommandHandlerResolver HandlerResolver => _handlerResolver ??= Services.GetRequiredService<CommandHandlerResolver>();
    protected Action<IEventCommand, Symbol> ChainIdSetter => _chainIdSetter ??= ChainIdSetterProperty.GetSetter<Symbol>();
    protected ILogger Log => _log ??= Services.LogFor(GetType());

    public Task Run(CommandContext context, CancellationToken cancellationToken = default)
    {
        if (context.UntypedCommand is IEventCommand { ChainId.IsEmpty: true } eventCommand)
            return RunEvent(eventCommand, (CommandContext<Unit>)context, cancellationToken);

        // Task.Run is used to call RunInternal to make sure parent
        // task's ExecutionContext won't be "polluted" by temp.
        // change of CommandContext.Current (via AsyncLocal).
        using var _ = context.IsOutermost ? ExecutionContextExt.TrySuppressFlow() : default;
        return Task.Run(() => RunCommand(context, cancellationToken), CancellationToken.None);
    }

    protected virtual async Task RunCommand(
        CommandContext context, CancellationToken cancellationToken = default)
    {
        try {
            var command = context.UntypedCommand;
            var handlers = HandlerResolver.GetHandlerChain(command);
            context.ExecutionState = new CommandExecutionState(handlers);
            if (handlers.Length == 0)
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

    protected virtual async Task RunEvent(
        IEventCommand command, CommandContext<Unit> context, CancellationToken cancellationToken = default)
    {
        try {
            if (!command.ChainId.IsEmpty)
                throw new ArgumentOutOfRangeException(nameof(command));

            var handlers = HandlerResolver.GetCommandHandlers(command);
            var handlerChains = handlers.HandlerChains;
            if (handlerChains.Count == 0) {
                await OnUnhandledEvent(command, context, cancellationToken).ConfigureAwait(false);
                return;
            }
            var callTasks = new Task[handlerChains.Count];
            var i = 0;
            foreach (var (chainId, _) in handlerChains) {
                var chainCommand = MemberwiseCloner.Invoke(command);
                ChainIdSetter.Invoke(chainCommand, chainId);
                callTasks[i++] = this.Call(chainCommand, context.IsOutermost, cancellationToken);
            }
            await Task.WhenAll(callTasks).ConfigureAwait(false);
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

    protected virtual Task OnUnhandledEvent(
        IEventCommand command, CommandContext<Unit> context,
        CancellationToken cancellationToken)
    {
        Log.LogWarning("Unhandled event: {Event}", command);
        return Task.CompletedTask;
    }
}
