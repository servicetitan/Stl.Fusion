namespace Stl.CommandR;

public static class ServerSideCommandExt
{
    public static TCommand MarkServerSide<TCommand>(this TCommand command)
        where TCommand : class, IServerSideCommand
    {
        command.IsServerSide = true;
        return command;
    }
}
