using Stl.Fusion.Extensions;

namespace Stl.Fusion.Tests.Extensions;

public class NestedOperationLoggerTester : IComputeService
{
    private IKeyValueStore KeyValueStore { get; }

    public NestedOperationLoggerTester(IKeyValueStore keyValueStore)
        => KeyValueStore = keyValueStore;

    [CommandHandler]
    public virtual async Task SetMany(NestedOperationLoggerTester_SetMany command, CancellationToken cancellationToken = default)
    {
        var (keys, valuePrefix) = command;
        var first = keys.FirstOrDefault();
        if (first == null)
            return;
        await KeyValueStore.Set(default, first, valuePrefix + keys.Length, cancellationToken);
        var nextCommand = new NestedOperationLoggerTester_SetMany(keys[1..], valuePrefix);
        var commander = this.GetCommander();
        await commander.Call(nextCommand, cancellationToken).ConfigureAwait(false);
    }
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
// ReSharper disable once InconsistentNaming
public partial record NestedOperationLoggerTester_SetMany(
    [property: DataMember, MemoryPackOrder(0)] string[] Keys,
    [property: DataMember, MemoryPackOrder(1)] string ValuePrefix
) : ICommand<Unit>;
