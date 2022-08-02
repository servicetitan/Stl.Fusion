namespace Stl.CommandR.Configuration;

public abstract record CommandHandler(
    Type CommandType,
    bool IsFilter = false,
    double Priority = 0)
{
    public abstract object GetHandlerService(
        ICommand command, CommandContext context);

    public abstract Task Invoke(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken);
}

public abstract record CommandHandler<TCommand> : CommandHandler
    where TCommand : class, ICommand
{
    protected CommandHandler(bool isFilter = false, double priority = 0)
        : base(typeof(TCommand), isFilter, priority) { }
}
