namespace Stl.CommandR;

public interface ICommander : IHasServices
{
    CommanderOptions Options { get; }

    Task Run(CommandContext context, CancellationToken cancellationToken = default);
}
