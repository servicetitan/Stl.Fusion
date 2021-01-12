using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using Stl.CommandR.Configuration;
using Stl.Fusion.Bridge.Interception;
using Stl.Fusion.Interception;

namespace Stl.Fusion.CommandR.Internal
{
    public class InvalidatingHandler : ICommandHandler<ICommand>
    {
        [CommandHandler(Order = -1000_000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            var finalHandler = context.ExecutionState.FindFinalHandler();
            var finalHandlerService = finalHandler?.GetHandlerService(command, context);
            var mustInvalidate = finalHandlerService is IComputeService
                && !(finalHandlerService is IReplicaService)
                && !Computed.IsInvalidating();

            if (mustInvalidate)
                await CommandAndInvalidate(command, context, false, cancellationToken).ConfigureAwait(false);
            else
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
        }

        protected static async Task CommandAndInvalidate(
            ICommand command, CommandContext context, bool invalidateOnError,
            CancellationToken cancellationToken)
        {
            var executionState = context.ExecutionState;
            if (invalidateOnError) {
                try {
                    await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                }
                finally {
                    context.ExecutionState = executionState;
                    using var _ = Computed.Invalidate();
                    await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                context.ExecutionState = executionState;
                using var _ = Computed.Invalidate();
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
