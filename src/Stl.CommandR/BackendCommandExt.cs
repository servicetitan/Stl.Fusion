namespace Stl.CommandR;

public static class BackendCommandExt
{
    public static TCommand MarkValid<TCommand>(this TCommand command)
        where TCommand : class, IBackendCommand
    {
        command.IsValid = true;
        return command;
    }
}
