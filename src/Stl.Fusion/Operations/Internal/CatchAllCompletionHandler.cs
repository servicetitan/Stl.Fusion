namespace Stl.Fusion.Operations.Internal;

public class CatchAllCompletionHandler : ICommandHandler<ICompletion>
{
    [CommandHandler(Priority = -1000_000_000, IsFilter = false)]
    public Task OnCommand(ICompletion command, CommandContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
