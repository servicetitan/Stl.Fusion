using System.Diagnostics;

namespace Stl.Fusion.Operations.Internal;

public class PostCompletionInvalidator : ICommandHandler<ICompletion>
{
    public record Options
    {
        public LogLevel LogLevel { get; init; } = LogLevel.None;
    }

    protected Options Settings { get; }
    protected ActivitySource ActivitySource { get; }
    protected InvalidationInfoProvider InvalidationInfoProvider { get; }
    protected ILogger Log { get; }
    protected bool IsLoggingEnabled { get; }

    public PostCompletionInvalidator(Options settings, IServiceProvider services)
    {
        Settings = settings;
        Log = services.LogFor(GetType());
        IsLoggingEnabled = Log.IsLogging(settings.LogLevel);

        ActivitySource = GetType().GetActivitySource();
        InvalidationInfoProvider = services.GetRequiredService<InvalidationInfoProvider>();
    }

    [CommandFilter(Priority = FusionOperationsCommandHandlerPriority.PostCompletionInvalidator)]
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
            using var activity = StartActivity(originalCommand);
            var finalHandler = context.ExecutionState.FindFinalHandler();
            var useOriginalCommandHandler = finalHandler == null
                || finalHandler.GetHandlerService(command, context) is CompletionTerminator;
            if (useOriginalCommandHandler) {
                if (InvalidationInfoProvider.IsReplicaServiceCommand(originalCommand)) {
                    if (IsLoggingEnabled)
                        Log.Log(Settings.LogLevel, "No invalidation for replica service command '{CommandType}'",
                            originalCommand.GetType());
                    return;
                }
                if (IsLoggingEnabled)
                    Log.Log(Settings.LogLevel, "Invalidating via original command handler for '{CommandType}'",
                        originalCommand.GetType());
                await context.Commander.Call(originalCommand, cancellationToken).ConfigureAwait(false);
            }
            else {
                if (IsLoggingEnabled)
                    Log.Log(Settings.LogLevel, "Invalidating via dedicated command handler for '{CommandType}'",
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

    protected virtual Activity? StartActivity(ICommand originalCommand)
    {
        var operationName = originalCommand.GetType().GetOperationName("Invalidate");
        var activity = ActivitySource.StartActivity(operationName);
        if (activity != null) {
            var tags = new ActivityTagsCollection { { "originalCommand", originalCommand.ToString() } };
            var activityEvent = new ActivityEvent(operationName, tags: tags);
            activity.AddEvent(activityEvent);
        }
        return activity;
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
