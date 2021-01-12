using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using Stl.CommandR.Configuration;

namespace Stl.Fusion.CommandR.Internal
{
    public class InvalidatingCommandHandler : ICommandHandler<ICommand>
    {
        [CommandHandler(Order = -1000_000, IsFilter = true)]
        public async Task OnCommandAsync(ICommand command, CommandContext context, CancellationToken cancellationToken)
        {
            if (Computed.IsInvalidating()) {
                await context.InvokeRemainingHandlersAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            var executionState = context.ExecutionState;
            if (command is IAlwaysInvalidatingCommand) {
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
