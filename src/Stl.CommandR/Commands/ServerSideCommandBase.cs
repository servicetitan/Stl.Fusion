using System.Text.Json.Serialization;
using Stl.CommandR.Internal;

namespace Stl.CommandR.Commands;

public interface IServerSideCommand : IPreparedCommand
{
    bool IsServerSide { get; set; }
}

public interface IServerSideCommand<TResult> : IServerSideCommand, ICommand<TResult>
{ }

[DataContract]
public abstract record ServerSideCommandBase<TResult> : IServerSideCommand<TResult>
{
    [JsonIgnore, IgnoreDataMember]
    [field: NonSerialized]
    public bool IsServerSide { get; set; }

    public virtual Task Prepare(CommandContext context, CancellationToken cancellationToken)
    {
        if (!IsServerSide)
            throw Errors.CommandIsServerSideOnly();
        return Task.CompletedTask;
    }
}
