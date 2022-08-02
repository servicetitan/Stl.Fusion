namespace Stl.CommandR.Internal;

public static class CommanderCommandHandlerPriority
{
    public const double PreparedCommandHandler = 1_000_000_000;
    public const double CommandTracer = 998_000_000;
    public const double LocalCommandRunner = 900_000_000;
}
