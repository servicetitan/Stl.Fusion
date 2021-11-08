using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Fusion.Operations.Internal;

public class InvalidateOnCompletionCommandHandler : ICommandHandler<ICompletion>
{
    public class Options
    {
        public bool IsLoggingEnabled { get; set; } = false;
    }

    protected InvalidationInfoProvider InvalidationInfoProvider { get; }
    protected ILogger Log { get; }
    protected bool IsLoggingEnabled { get; set; }
    protected LogLevel LogLevel { get; set; } = LogLevel.Information;

    public InvalidateOnCompletionCommandHandler(Options? options,
        InvalidationInfoProvider invalidationInfoProvider,
        ILogger<InvalidateOnCompletionCommandHandler>? log = null)
    {
        options ??= new();
        Log = log ?? NullLogger<InvalidateOnCompletionCommandHandler>.Instance;
        IsLoggingEnabled = options.IsLoggingEnabled && Log.IsEnabled(LogLevel);

        InvalidationInfoProvider = invalidationInfoProvider;
    }

    [CommandHandler(Priority = 100, IsFilter = true)]
    public async Task OnCommand(ICompletion command, CommandContext context, CancellationToken cancellationToken)
    {
        var originalCommand = command.UntypedCommand;
        var requiresInvalidation =
            InvalidationInfoProvider.RequiresInvalidation(originalCommand)
            && !Computed.IsInvalidating();
        if (!requiresInvalidation) {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
            return;
        }

        var oldOperation = context.Items.Get<IOperation>();
        var operation = command.Operation;
        context.SetOperation(operation);
        var invalidateScope = Computed.Invalidate();
        try {
            var finalHandler = context.ExecutionState.FindFinalHandler();
            var useOriginalCommandHandler = finalHandler == null
                || finalHandler.GetHandlerService(command, context) is CatchAllCompletionHandler;
            if (useOriginalCommandHandler) {
                if (InvalidationInfoProvider.IsReplicaServiceCommand(originalCommand)) {
                    if (IsLoggingEnabled)
                        Log.Log(LogLevel, "No invalidation for replica service command '{CommandType}'",
                            originalCommand.GetType());
                    return;
                }
                if (IsLoggingEnabled)
                    Log.Log(LogLevel, "Invalidating via original command handler for '{CommandType}'",
                        originalCommand.GetType());
                await context.Commander.Call(originalCommand, cancellationToken).ConfigureAwait(false);
            }
            else {
                if (IsLoggingEnabled)
                    Log.Log(LogLevel, "Invalidating via dedicated command handler for '{CommandType}'",
                        command.GetType());
                await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
            }

            var operationItems = operation.Items;
            try {
                var nestedCommands = operationItems.GetOrDefault(ImmutableList<NestedCommandEntry>.Empty);
                if (!nestedCommands.IsEmpty)
                    await InvokeNestedCommands(context, operation, nestedCommands, cancellationToken).ConfigureAwait(false);
            }
            finally {
                operation.Items = operationItems;
            }
        }
        finally {
            context.SetOperation(oldOperation);
            invalidateScope.Dispose();
        }
    }

    protected virtual async ValueTask InvokeNestedCommands(
        CommandContext context,
        IOperation operation,
        ImmutableList<NestedCommandEntry> nestedCommands,
        CancellationToken cancellationToken)
    {
        foreach (var commandEntry in nestedCommands) {
            var (command, items) = commandEntry;
            // if (command is IBackendCommand backendCommand)
            //     backendCommand.MarkValid();
            if (InvalidationInfoProvider.RequiresInvalidation(command)) {
                operation.Items = items;
                await context.Commander.Call(command, cancellationToken).ConfigureAwait(false);
            }
            var subcommands = items.GetOrDefault(ImmutableList<NestedCommandEntry>.Empty);
            if (!subcommands.IsEmpty)
                await InvokeNestedCommands(context, operation, subcommands, cancellationToken).ConfigureAwait(false);
        }
    }
}
