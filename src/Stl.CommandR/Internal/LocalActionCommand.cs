using System.Text.Json.Serialization;

namespace Stl.CommandR.Internal;

public record LocalActionCommand : LocalCommand, ICommand<Unit>
{
    [IgnoreDataMember, JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public Func<CancellationToken, Task>? Handler { get; init; }

    public override Task Run(CancellationToken cancellationToken)
    {
        if (Handler == null)
            throw Errors.LocalCommandHasNoHandler();
        return Handler(cancellationToken);
    }
}
