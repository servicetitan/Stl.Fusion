namespace Stl.CommandR.Internal;

public class LocalCommandRunner : ICommandHandler<ILocalCommand>
{
    [CommandHandler(Priority = CommanderCommandHandlerPriority.LocalCommandRunner)]
    public Task OnCommand(
        ILocalCommand command, CommandContext context,
        CancellationToken cancellationToken)
        => command.Run(cancellationToken);
}
