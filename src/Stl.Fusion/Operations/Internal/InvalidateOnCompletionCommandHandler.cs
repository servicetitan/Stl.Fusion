using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Collections;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.Operations.Internal
{
    public class InvalidateOnCompletionCommandHandler : ICommandHandler<ICompletion>
    {
        public class Options
        {
            public LogLevel LogLevel { get; set; } = LogLevel.None;
        }

        protected InvalidationInfoProvider InvalidationInfoProvider { get; }
        protected LogLevel LogLevel { get; }
        protected ILogger Log { get; }

        public InvalidateOnCompletionCommandHandler(Options? options,
            InvalidationInfoProvider invalidationInfoProvider,
            ILogger<InvalidateOnCompletionCommandHandler>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<InvalidateOnCompletionCommandHandler>.Instance;
            LogLevel = options.LogLevel;
            InvalidationInfoProvider = invalidationInfoProvider;
        }

        [CommandHandler(Priority = 100, IsFilter = true)]
        public async Task OnCommandAsync(ICompletion command, CommandContext context, CancellationToken cancellationToken)
        {
            var originalCommand = command.UntypedCommand;
            var requiresInvalidation =
                InvalidationInfoProvider.RequiresInvalidation(originalCommand)
                && !Computed.IsInvalidating();
            if (!requiresInvalidation) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var oldOperation = context.Items.TryGet<IOperation>();
            var operation = command.Operation;
            context.SetOperation(operation);
            var invalidateScope = Computed.Invalidate();
            try {
                var logEnabled = LogLevel != LogLevel.None && Log.IsEnabled(LogLevel);
                var finalHandler = context.ExecutionState.FindFinalHandler();
                if (finalHandler != null) {
                    if (logEnabled)
                        Log.Log(LogLevel, "Invalidating via dedicated command handler for '{CommandType}'", command.GetType());
                    await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                }
                else {
                    if (logEnabled)
                        Log.Log(LogLevel, "Invalidating via shared command handler for '{CommandType}'", originalCommand.GetType());
                    await context.Commander.CallAsync(originalCommand, cancellationToken).ConfigureAwait(false);
                }

                var operationItems = operation.Items;
                try {
                    var nestedCommands = operationItems.GetOrDefault(ImmutableList<NestedCommandEntry>.Empty);
                    if (!nestedCommands.IsEmpty)
                        await InvokeNestedCommandsAsync(context, operation, nestedCommands, cancellationToken);
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

        protected virtual async ValueTask InvokeNestedCommandsAsync(
            CommandContext context,
            IOperation operation,
            ImmutableList<NestedCommandEntry> nestedCommands,
            CancellationToken cancellationToken)
        {
            foreach (var commandEntry in nestedCommands) {
                var (command, items) = commandEntry;
                if (command is IServerSideCommand serverSideCommand)
                    serverSideCommand.MarkServerSide(); // Server-side commands should be marked as such
                if (InvalidationInfoProvider.RequiresInvalidation(command)) {
                    operation.Items = items;
                    await context.Commander.CallAsync(command, cancellationToken).ConfigureAwait(false);
                }
                var subcommands = items.GetOrDefault(ImmutableList<NestedCommandEntry>.Empty);
                if (!subcommands.IsEmpty)
                    await InvokeNestedCommandsAsync(context, operation, subcommands, cancellationToken);
            }
        }
    }
}
