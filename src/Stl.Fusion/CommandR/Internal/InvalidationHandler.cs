using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.CommandR.Internal
{
    public class InvalidationHandler : ICommandHandler<ICommand>, ICommandHandler<IInvalidate>
    {
        public class Options
        {
            public bool IsEnabled { get; set; } = true;
        }

        protected IInvalidationInfoProvider InvalidationInfoProvider { get; }
        protected ILogger Log { get; }

        public bool IsEnabled { get; }

        public InvalidationHandler(Options? options,
            IInvalidationInfoProvider invalidationInfoProvider,
            ILogger<InvalidationHandler>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<InvalidationHandler>.Instance;
            IsEnabled = options.IsEnabled;
            InvalidationInfoProvider = invalidationInfoProvider;
        }

        [CommandHandler(Order = -1000_000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var skip = !IsEnabled
                || context.OuterContext != null // Should be top-level command
                || command is IInvalidate // Second handler here will take care of it
                || Computed.IsInvalidating();
            if (skip) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            if (InvalidationInfoProvider.RequiresInvalidation(command))
                context.Items.Set(Invalidate.New(command));

            await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);

            var invalidate = context.Items.TryGet<IInvalidate>();
            if (invalidate != null)
                await context.Commander.RunAsync(invalidate, true, default).ConfigureAwait(false);
        }

        [CommandHandler(Order = -1000_001, IsFilter = true)]
        public async Task OnCommandAsync(IInvalidate command, CommandContext context, CancellationToken cancellationToken)
        {
            var skip = !IsEnabled
                || !InvalidationInfoProvider.RequiresInvalidation(command.UntypedCommand)
                || Computed.IsInvalidating();
            if (skip) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            using var _ = Computed.Invalidate();
            var finalHandler = context.ExecutionState.FindFinalHandler();
            if (finalHandler != null) {
                if (Log.IsEnabled(LogLevel.Debug))
                    Log.LogDebug("Invalidating via dedicated command handler: {0}", command);
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
            }
            else {
                if (Log.IsEnabled(LogLevel.Debug))
                    Log.LogDebug("Invalidating via shared command handler: {0}", command);
                await context.Commander.RunAsync(command.UntypedCommand, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
