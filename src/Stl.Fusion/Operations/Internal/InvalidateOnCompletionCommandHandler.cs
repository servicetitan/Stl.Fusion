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
            var requiredInvalidation =
                InvalidationInfoProvider.RequiresInvalidation(originalCommand)
                && !Computed.IsInvalidating();
            if (!requiredInvalidation) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var oldOperation = context.Items.TryGet<IOperation>();
            context.SetOperation(command.Operation);
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
                    await context.Commander.RunAsync(originalCommand, cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                context.SetOperation(oldOperation);
                invalidateScope.Dispose();
            }
        }
    }
}
