namespace Stl.CommandR.Configuration;

public sealed class CommandHandlerRegistry(IServiceProvider services)
{
    public IReadOnlyList<CommandHandler> Handlers { get; } =
        services.GetRequiredService<HashSet<CommandHandler>>().ToArray();
}
