using Stl.Fusion.Extensions;

namespace Stl.Fusion.Tests.Extensions;

[RegisterComputeService(Scope = ServiceScope.Services)]
public class NestedOperationLoggerTester : IComputeService
{
    [DataContract]
    public record SetManyCommand(
        [property: DataMember] string[] Keys,
        [property: DataMember] string ValuePrefix
        ) : ICommand<Unit>
    {
        public SetManyCommand() : this(Array.Empty<string>(), "") { }
    }

    private IKeyValueStore KeyValueStore { get; }

    public NestedOperationLoggerTester(IKeyValueStore keyValueStore)
        => KeyValueStore = keyValueStore;

    [CommandHandler]
    public virtual async Task SetMany(SetManyCommand command, CancellationToken cancellationToken = default)
    {
        var (keys, valuePrefix) = command;
        var first = keys.FirstOrDefault();
        if (first == null)
            return;
        await KeyValueStore.Set(default, first, valuePrefix + keys.Length, cancellationToken);
        var nextCommand = new SetManyCommand(keys[1..], valuePrefix);
        await SetMany(nextCommand, cancellationToken).ConfigureAwait(false);
    }
}
