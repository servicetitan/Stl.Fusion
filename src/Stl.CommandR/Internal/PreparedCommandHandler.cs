namespace Stl.CommandR.Internal;

public class PreparedCommandHandler :
    ICommandHandler<IPreparedCommand>
{
    [CommandHandler(Priority = 1000_000_000, IsFilter = true)]
    public async Task OnCommand(IPreparedCommand command, CommandContext context, CancellationToken cancellationToken)
    {
        await command.Prepare(context, cancellationToken).ConfigureAwait(false);
        await context.InvokeRemainingHandlers(cancellationToken).ConfigureAwait(false);
    }
}
