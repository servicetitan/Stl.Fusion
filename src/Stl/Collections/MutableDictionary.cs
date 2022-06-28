using System.Diagnostics.CodeAnalysis;

namespace Stl.Collections;

public interface IReadOnlyMutableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    ImmutableDictionary<TKey, TValue> Items { get; }
    Task WhenChanged { get; }
    event Action? Changed;
}

public interface IMutableDictionary<TKey, TValue> : IReadOnlyMutableDictionary<TKey, TValue>
    where TKey : notnull
{
    new ImmutableDictionary<TKey, TValue> Items { get; set; }

    bool SetItems(ImmutableDictionary<TKey, TValue> items, ImmutableDictionary<TKey, TValue> expectedItems);
    bool SetItems(ImmutableDictionary<TKey, TValue> items);
}

public sealed class MutableDictionary<TKey, TValue> : IMutableDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly object _lock = new();
    private volatile Task<Unit> _whenChangedTask;
    private volatile ImmutableDictionary<TKey, TValue> _items;

    public ImmutableDictionary<TKey, TValue> Items {
        get => _items;
        set => SetItems(value);
    }

    public Task WhenChanged => _whenChangedTask;
    public event Action? Changed;

    public int Count => _items.Count;
    public TValue this[TKey key] => _items[key];
    public IEnumerable<TKey> Keys => _items.Keys;
    public IEnumerable<TValue> Values => _items.Values;

    public MutableDictionary() : this(ImmutableDictionary<TKey, TValue>.Empty) { }
    public MutableDictionary(ImmutableDictionary<TKey, TValue> items)
    {
        _whenChangedTask = TaskSource.New<Unit>(true).Task;
        _items = items;
    }

    public bool SetItems(ImmutableDictionary<TKey, TValue> items, ImmutableDictionary<TKey, TValue> expectedItems)
    {
        lock (_lock) {
            if (_items != expectedItems || _items == items)
                return false;

            _items = items;
            var taskSource = TaskSource.For(_whenChangedTask);
            _whenChangedTask = TaskSource.New<Unit>(true).Task;
            taskSource.TrySetResult(default);
        }
        Changed?.Invoke();
        return true;
    }

    public bool SetItems(ImmutableDictionary<TKey, TValue> items)
    {
        lock (_lock) {
            if (_items == items)
                return false;

            _items = items;
            var taskSource = TaskSource.For(_whenChangedTask);
            _whenChangedTask = TaskSource.New<Unit>(true).Task;
            taskSource.TrySetResult(default);
        }
        Changed?.Invoke();
        return true;
    }

    // IReadOnlyDictionary members

    IEnumerator IEnumerable.GetEnumerator() 
        => GetEnumerator();
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() 
        => Items.GetEnumerator();
    public bool ContainsKey(TKey key) 
        => Items.ContainsKey(key);
#pragma warning disable CS8767
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => Items.TryGetValue(key, out value);
#pragma warning restore CS8767
}
