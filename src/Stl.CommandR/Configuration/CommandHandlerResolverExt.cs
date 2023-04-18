namespace Stl.CommandR.Configuration;

public static class CommandHandlerResolverExt
{
    public static CommandHandlerSet GetCommandHandlers(this CommandHandlerResolver resolver, ICommand command)
        => resolver.GetCommandHandlers(command.GetType());

    public static ImmutableArray<CommandHandler> GetHandlerChain(this CommandHandlerResolver resolver, ICommand command)
        => resolver.GetCommandHandlers(command.GetType()).GetHandlerChain(command);
}
