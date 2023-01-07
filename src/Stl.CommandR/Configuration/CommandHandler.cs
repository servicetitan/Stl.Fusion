namespace Stl.CommandR.Configuration;

public abstract record CommandHandler(
    Symbol Id,
    Type CommandType,
    bool IsFilter = false,
    double Priority = 0)
{
    public abstract object GetHandlerService(
        ICommand command, CommandContext context);

    public abstract Task Invoke(
        ICommand command, CommandContext context,
        CancellationToken cancellationToken);

    public override string ToString()
        => $"{Id.Value}[Priority = {Priority}{(IsFilter ? ", IsFilter = true" : "")}]";

    // This record relies on reference-based equality
    public virtual bool Equals(CommandHandler? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}

public abstract record CommandHandler<TCommand> : CommandHandler
    where TCommand : class, ICommand
{
    protected CommandHandler(Symbol id, bool isFilter = false, double priority = 0)
        : base(id, typeof(TCommand), isFilter, priority) { }

    public override string ToString() => base.ToString();

    // This record relies on reference-based equality
    public virtual bool Equals(CommandHandler<TCommand>? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}
