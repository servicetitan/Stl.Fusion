namespace Stl.CommandR.Internal;

public sealed record LocalActionCommand : LocalCommand, ICommand<Unit>
{
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember]
    public Func<CancellationToken, Task>? Handler { get; init; }

    public override Task Run(CancellationToken cancellationToken)
    {
        if (Handler == null)
            throw Errors.LocalCommandHasNoHandler();

        return Handler(cancellationToken);
    }
}
