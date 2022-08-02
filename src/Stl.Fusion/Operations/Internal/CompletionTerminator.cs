namespace Stl.Fusion.Operations.Internal;

public class CompletionTerminator : ICommandHandler<ICompletion>
{
    [CommandHandler(Priority = FusionOperationsCommandHandlerPriority.CompletionTerminator, IsFilter = false)]
    public Task OnCommand(ICompletion command, CommandContext context, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
