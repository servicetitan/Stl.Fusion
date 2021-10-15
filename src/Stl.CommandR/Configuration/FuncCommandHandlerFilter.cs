namespace Stl.CommandR.Configuration;

public class FuncCommandHandlerFilter : CommandHandlerFilter
{
    Func<CommandHandler, Type, bool> Filter { get; }

    public FuncCommandHandlerFilter(Func<CommandHandler, Type, bool> filter)
        => Filter = filter;

    public override bool IsCommandHandlerUsed(CommandHandler commandHandler, Type commandType)
        => Filter.Invoke(commandHandler, commandType);
}
