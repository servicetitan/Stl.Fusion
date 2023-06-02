namespace Stl.CommandR;

public interface ICommander : IHasServices
{
    Task Run(CommandContext context, CancellationToken cancellationToken = default);
}
