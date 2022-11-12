namespace Stl.Fusion.Operations.Internal;

/// <summary>
/// This handler captures invocations of nested commands inside
/// operations and logs them into context.Operation().Items
/// so that invalidation for them could be auto-replayed too.
/// </summary>
public class NestedCommandLogger : ICommandHandler<ICommand>
{
    protected InvalidationInfoProvider InvalidationInfoProvider { get; }
    protected ILogger Log { get; }

    public NestedCommandLogger(
        InvalidationInfoProvider invalidationInfoProvider,
        ILogger<NestedCommandLogger>? log = null)
    {
        Log = log ?? NullLogger<NestedCommandLogger>.Instance;
        InvalidationInfoProvider = invalidationInfoProvider;
    }

    [CommandHandler(Priority = FusionOperationsCommandHandlerPriority.NestedCommandLogger, IsFilter = true)]
    public async Task OnCommand(ICommand command, CommandContext context, CancellationToken cancellationToken)
    {
        var operation = context.OuterContext != null ? context.Items.Get<IOperation>() : null;
        var mustBeLogged =
            operation != null // Should be a nested context inside a context w/ operation
            && InvalidationInfoProvider.RequiresInvalidation(command) // Command requires invalidation
            && !Computed.IsInvalidating();
        if (!mustBeLogged) {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
            return;
        }

        var operationItems = operation!.Items;
        var commandItems = new OptionSet();
        operation.Items = commandItems;
        Exception? error = null;
        try {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception? e) {
            error = e;
            throw;
        }
        finally {
            operation.Items = operationItems;
            if (error == null) {
                var newOperation = context.Operation();
                if (newOperation != operation) {
                    // The operation might be changed by nested command in case
                    // it's the one that started to use DbOperationScope first
                    commandItems = newOperation.Items;
                    newOperation.Items = operationItems = new OptionSet();
                }
                var nestedCommands = operationItems.Get(ImmutableList<NestedCommandEntry>.Empty);
                nestedCommands = nestedCommands.Add(new NestedCommandEntry(command, commandItems));
                operationItems.Set(nestedCommands);
            }
        }
    }
}
