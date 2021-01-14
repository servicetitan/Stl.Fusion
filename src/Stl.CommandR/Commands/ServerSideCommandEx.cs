namespace Stl.CommandR.Commands
{
    public static class ServerSideCommandEx
    {
        public static TCommand MarkServerSide<TCommand>(this TCommand command)
            where TCommand : class, IServerSideCommand
        {
            command.MarkServerSide(true);
            return command;
        }
    }
}
