namespace Stl.CommandR.Internal;

public class Commander : ICommander
{
    protected ILogger Log { get; init; }
    protected ICommandHandlerResolver HandlerResolver { get; }
    protected Action<IMultiChainCommand, Symbol> ChainIdSetter { get; }

    public CommanderOptions Options { get; }
    public IServiceProvider Services { get; }

    public Commander(CommanderOptions options, IServiceProvider services)
    {
        Options = options;
        Services = services;
        Log = Services.LogFor(GetType());
        HandlerResolver = services.GetRequiredService<ICommandHandlerResolver>();
        ChainIdSetter = typeof(IMultiChainCommand)
            .GetProperty(nameof(IMultiChainCommand.ChainId))!
            .GetSetter<Symbol>();
    }

    public Task Run(CommandContext context, CancellationToken cancellationToken = default)
    {
        // Task.Run is used to call RunInternal to make sure parent
        // task's ExecutionContext won't be "polluted" by temp.
        // change of CommandContext.Current (via AsyncLocal).
        var isSingleChain = context.UntypedCommand is not IMultiChainCommand mc || !mc.ChainId.IsEmpty;
        if (isSingleChain) {
            using var _ = context.IsOutermost ? ExecutionContextExt.SuppressFlow() : default;
            return Task.Run(() => RunSingleChain(context, cancellationToken), CancellationToken.None);
        }

        return RunMultipleChains(context, cancellationToken);
    }

    protected virtual async Task RunMultipleChains(
        CommandContext context, CancellationToken cancellationToken = default)
    {
        try {
            var command = (IMultiChainCommand)context.UntypedCommand;
            var typedContext = (CommandContext<Unit>)context;
            if (!command.ChainId.IsEmpty)
                throw new ArgumentOutOfRangeException(nameof(command));

            var handlers = HandlerResolver.GetCommandHandlers(command.GetType());
            var multipleChains = handlers.MultipleChains;
            var chainTasks = new Task[multipleChains.Count];
            var i = 0;
            foreach (var (chainId, _) in multipleChains) {
                var chainCommand = MemberwiseCloner.Invoke(command);
                ChainIdSetter.Invoke(chainCommand, chainId);
                chainTasks[i++] = this.Call(chainCommand, context.IsOutermost, cancellationToken);
            }
            await Task.WhenAll(chainTasks).ConfigureAwait(false);
            typedContext.SetResult(default);
        }
        catch (Exception e) {
            context.SetResult(e);
        }
        finally {
            context.TryComplete(cancellationToken);
            await context.DisposeAsync().ConfigureAwait(false);
        }
    }

    protected virtual async Task RunSingleChain(
        CommandContext context, CancellationToken cancellationToken = default)
    {
        try {
            var command = context.UntypedCommand;
            var handlers = HandlerResolver
                .GetCommandHandlers(command.GetType())
                .GetHandlerChain(command);
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

    protected virtual Task OnUnhandledCommand(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken)
        => throw Errors.NoHandlerFound(command.GetType());
}
