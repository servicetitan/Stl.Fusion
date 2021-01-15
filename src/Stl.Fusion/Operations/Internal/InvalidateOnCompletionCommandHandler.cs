using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.Operations.Internal
{
    public class InvalidateOnCompletionCommandHandler : ICommandHandler<ICompletion>
    {
        public class Options
        {
            public LogLevel LogLevel { get; set; } = LogLevel.None;
        }

        protected IInvalidationInfoProvider InvalidationInfoProvider { get; }
        protected LogLevel LogLevel { get; }
        protected ILogger Log { get; }

        public InvalidateOnCompletionCommandHandler(Options? options,
            IInvalidationInfoProvider invalidationInfoProvider,
            ILogger<InvalidateOnCompletionCommandHandler>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<InvalidateOnCompletionCommandHandler>.Instance;
            LogLevel = options.LogLevel;
            InvalidationInfoProvider = invalidationInfoProvider;
        }

        [CommandHandler(Priority = 10_001, IsFilter = true)]
        public async Task OnCommandAsync(ICompletion command, CommandContext context, CancellationToken cancellationToken)
        {
            var requiredInvalidation =
                InvalidationInfoProvider.RequiresInvalidation(command.UntypedCommand)
                && !Computed.IsInvalidating();
            if (!requiredInvalidation) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            // Copying IOperation.Items to CommandContext.Items
            var operation = command.Operation;
            var operationItems = operation?.Items.Items;
            if (operationItems != null) {
                foreach (var (key, value) in operationItems) {
                    if (value is IOperationItem)
                        context.Items[key] = value;
                }
            }

            var logEnabled = LogLevel != LogLevel.None && Log.IsEnabled(LogLevel);
            using var _ = Computed.Invalidate();

            var finalHandler = context.ExecutionState.FindFinalHandler();
            if (finalHandler != null) {
                if (logEnabled)
                    Log.Log(LogLevel, "Invalidating via dedicated command handler: {0}", command);
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
            }
            else {
                if (logEnabled)
                    Log.Log(LogLevel, "Invalidating via shared command handler: {0}", command);
                await context.Commander.RunAsync(command.UntypedCommand, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
