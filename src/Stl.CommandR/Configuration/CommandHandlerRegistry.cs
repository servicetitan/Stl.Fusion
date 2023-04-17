namespace Stl.CommandR.Configuration;

public sealed class CommandHandlerRegistry
{
    public IReadOnlyList<CommandHandler> Handlers { get; }

    public CommandHandlerRegistry(IServiceProvider services)
        => Handlers = services.GetRequiredService<HashSet<CommandHandler>>().ToArray();
}
