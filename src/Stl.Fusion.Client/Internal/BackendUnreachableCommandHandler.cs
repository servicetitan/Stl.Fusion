using Stl.Fusion.Bridge.Interception;

namespace Stl.Fusion.Client.Internal;

/// <summary>
/// This handler just makes "TypeError: Failed to fetch" errors more descriptive.
/// </summary>
public class BackendUnreachableCommandHandler : ICommandHandler<ICommand>
{
    [CommandHandler(Priority = 100_000_000, IsFilter = true)]
    public async Task OnCommand(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken)
    {
        if (command is IMetaCommand) goto Skip;

        var finalHandler = context.ExecutionState.FindFinalHandler();
        if (finalHandler?.GetHandlerService(command, context) is not IReplicaService)
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
