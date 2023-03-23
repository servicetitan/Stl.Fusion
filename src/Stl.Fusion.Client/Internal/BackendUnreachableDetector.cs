using Stl.Interception.Internal;

namespace Stl.Fusion.Client.Internal;

/// <summary>
/// This handler just makes "TypeError: Failed to fetch" errors more descriptive.
/// </summary>
public class BackendUnreachableDetector : ICommandHandler<ICommand>
{
    [CommandFilter(Priority = FusionClientCommandHandlerPriority.BackendUnreachableDetector)]
    public async Task OnCommand(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken)
    {
        if (command is IMetaCommand) goto Skip;

        var finalHandler = context.ExecutionState.FindFinalHandler();
        var finalHandlerService = finalHandler?.GetHandlerService(command, context);
        if (finalHandlerService is not IComputeService cs || !cs.IsReplicaService())
            goto Skip;

        try {
            await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e) {
            var isFailedToFetch = StringComparer.Ordinal.Equals(e.Message, "TypeError: Failed to fetch");
            if (isFailedToFetch)
                throw Errors.BackendIsUnreachable(e);
            throw;
        }
        return;
    Skip:
        await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
    }
}
