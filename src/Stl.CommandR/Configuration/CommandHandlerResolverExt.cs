namespace Stl.CommandR.Configuration;

public static class CommandHandlerResolverExt
{
    public static CommandHandlerSet GetCommandHandlers(this ICommandHandlerResolver resolver, ICommand command)
        => resolver.GetCommandHandlers(command.GetType());

    public static ImmutableArray<CommandHandler> GetHandlerChain(this ICommandHandlerResolver resolver, ICommand command)
        => resolver.GetCommandHandlers(command.GetType()).GetHandlerChain(command);
}
