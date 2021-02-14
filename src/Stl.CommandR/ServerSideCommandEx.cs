using Stl.CommandR.Commands;

namespace Stl.CommandR
{
    public static class ServerSideCommandEx
    {
        public static TCommand MarkServerSide<TCommand>(this TCommand command)
            where TCommand : class, IServerSideCommand
        {
            command.IsServerSide = true;
            return command;
        }
    }
}
