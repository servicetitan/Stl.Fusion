using System.Collections.Concurrent;
using MemoryPack;
using Stl.Reflection;

namespace Stl.Fusion.Tests.Services;

public interface IKeyValueService<TValue> : IComputeService
{
    [DataContract, MemoryPackable]
    public partial record SetCommand(
        [property: DataMember] string Key,
        [property: DataMember] TValue Value
    ) : ICommand<Unit>;

    [DataContract, MemoryPackable]
    public partial record RemoveCommand(
        [property: DataMember] string Key
    ) : ICommand<Unit>;

    [ComputeMethod]
    Task<Option<TValue>> TryGet(string key, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<TValue> Get(string key, CancellationToken cancellationToken = default);
    Task Set(string key, TValue value, CancellationToken cancellationToken = default);
    Task Remove(string key, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task SetCmd(SetCommand cmd, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task RemoveCmd(RemoveCommand cmd, CancellationToken cancellationToken = default);
}

public class KeyValueService<TValue> : IKeyValueService<TValue>
{
    private readonly ConcurrentDictionary<string, TValue> _values = new(StringComparer.Ordinal);

    private ICommander Commander { get; }

    public KeyValueService(IServiceProvider services)
    {
        Commander = services.Commander();
        Debug.WriteLine($"{GetType().GetName()} created @ {services.GetHashCode()}.");
    }

    public virtual Task<Option<TValue>> TryGet(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_values.TryGetValue(key, out var v) ? Option.Some(v) : default);

#pragma warning disable 1998
    public virtual async Task<TValue> Get(string key, CancellationToken cancellationToken = default)
#pragma warning restore 1998
    {
        if (key.EndsWith("error"))
            throw new ArgumentException("Error!", nameof(key));
        return _values.TryGetValue(key, out var value) ? value : default!;
    }

    public Task Set(string key, TValue value, CancellationToken cancellationToken = default)
        => Commander.Call(new IKeyValueService<TValue>.SetCommand(key, value), cancellationToken);

    public Task Remove(string key, CancellationToken cancellationToken = default)
        => Commander.Call(new IKeyValueService<TValue>.RemoveCommand(key), cancellationToken);

    public virtual Task SetCmd(IKeyValueService<TValue>.SetCommand cmd, CancellationToken cancellationToken = default)
    {
        _values[cmd.Key] = cmd.Value;

        if (Computed.IsInvalidating()) {
            _ = TryGet(cmd.Key, default).AssertCompleted();
            _ = Get(cmd.Key, default).AssertCompleted();
        }
        return Task.CompletedTask;
    }

    public virtual Task RemoveCmd(IKeyValueService<TValue>.RemoveCommand cmd, CancellationToken cancellationToken = default)
    {
        _values.TryRemove(cmd.Key, out _);

        if (Computed.IsInvalidating()) {
            _ = TryGet(cmd.Key, default).AssertCompleted();
            _ = Get(cmd.Key, default).AssertCompleted();
        }
        return Task.CompletedTask;
    }
}
