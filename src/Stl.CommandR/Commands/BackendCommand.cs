using System.Text.Json.Serialization;
using Stl.CommandR.Internal;

namespace Stl.CommandR.Commands;

public interface IBackendCommand : IPreparedCommand
{
    bool IsValid { get; set; }
}

public interface IBackendCommand<TResult> : IBackendCommand, ICommand<TResult>
{ }

[DataContract]
public abstract record BackendCommand<TResult> : IBackendCommand<TResult>
{
    [JsonIgnore, IgnoreDataMember]
    [field: NonSerialized]
    public bool IsValid { get; set; }

    public virtual Task Prepare(CommandContext context, CancellationToken cancellationToken)
    {
        if (!IsValid)
            throw Errors.BackendCommandMustBeStartedOnBackend();
        return Task.CompletedTask;
    }
}
