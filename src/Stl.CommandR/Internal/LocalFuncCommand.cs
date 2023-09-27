namespace Stl.CommandR.Internal;

public sealed record LocalFuncCommand<T> : LocalCommand, ICommand<T>
{
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember]
    public Func<CancellationToken, Task<T>>? Handler { get; init; }

    public override async Task Run(CancellationToken cancellationToken)
    {
        if (Handler == null)
            throw Errors.LocalCommandHasNoHandler();

        var context = CommandContext.GetCurrent<T>();
        var result = await Handler(cancellationToken).ConfigureAwait(false);
        context.SetResult(result);
    }
}
