namespace Stl.CommandR.Internal;

public class LocalCommandHandler : ICommandHandler<ILocalCommand>
{
    [CommandHandler(Priority = 900_000_000)]
    public Task OnCommand(
        ILocalCommand command, CommandContext context,
        CancellationToken cancellationToken)
        => command.Run(cancellationToken);
}
