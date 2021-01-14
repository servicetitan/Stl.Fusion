using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.Operations.Internal
{
    public class InvalidationHandler : ICommandHandler<ICommand>, ICommandHandler<IInvalidateCommand>
    {
        public class Options
        {
            public bool IsEnabled { get; set; } = true;
            public LogLevel LogLevel { get; set; } = LogLevel.None;
        }

        protected IInvalidationInfoProvider InvalidationInfoProvider { get; }
        protected LogLevel LogLevel { get; }
        protected ILogger Log { get; }

        public bool IsEnabled { get; }

        public InvalidationHandler(Options? options,
            IInvalidationInfoProvider invalidationInfoProvider,
            ILogger<InvalidationHandler>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<InvalidationHandler>.Instance;
            LogLevel = options.LogLevel;
            IsEnabled = options.IsEnabled;
            InvalidationInfoProvider = invalidationInfoProvider;
        }

        [CommandHandler(Order = -10_000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var skip = !IsEnabled
                || context.OuterContext != null // Should be top-level command
                || command is IInvalidateCommand // Second handler here will take care of it
                || Computed.IsInvalidating();
            if (skip) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);

            if (InvalidationInfoProvider.RequiresInvalidation(command)) {
                var invalidateCommand = context.Items.TryGet<IInvalidateCommand>() ?? InvalidateCommand.New(command);
                await context.Commander.RunAsync(invalidateCommand, true, default).ConfigureAwait(false);
            }
        }

        [CommandHandler(Order = -10_001, IsFilter = true)]
        public async Task OnCommandAsync(IInvalidateCommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var skip = !IsEnabled
                || !InvalidationInfoProvider.RequiresInvalidation(command.UntypedCommand)
                || Computed.IsInvalidating();
            if (skip) {
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
