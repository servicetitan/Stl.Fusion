namespace Stl.CommandR;

public interface ICommandHandler
{ }

public interface ICommandHandler<in TCommand> : ICommandHandler
    where TCommand : class, ICommand
{
    Task OnCommand(
        TCommand command, CommandContext context,
        CancellationToken cancellationToken);
}
