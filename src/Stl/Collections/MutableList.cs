namespace Stl.Collections;

#pragma warning disable CA1710

public interface IReadOnlyMutableList<T> : IReadOnlyCollection<T>
{
    ImmutableList<T> Items { get; }
    Task WhenChanged { get; }
    event Action? Changed;
}

public interface IMutableList<T> : IReadOnlyMutableList<T>, IList<T>
{
    new ImmutableList<T> Items { get; set; }

    bool Update(ImmutableList<T> items);
    bool Update(ImmutableList<T> items, ImmutableList<T> expectedItems);
    bool Update(Func<ImmutableList<T>, ImmutableList<T>> updater);
    bool Update<TState>(TState state, Func<TState, ImmutableList<T>, ImmutableList<T>> updater);
}

public class MutableList<T>(ImmutableList<T> items) : IMutableList<T>
{
    private readonly object _lock = new();
    private volatile TaskCompletionSource<Unit> _whenChangedSource = TaskCompletionSourceExt.New<Unit>();
    private volatile ImmutableList<T> _items = items;

    public ImmutableList<T> Items {
        get => _items;
        set => Update(value);
    }

    public Task WhenChanged => _whenChangedSource.Task;
    public event Action? Changed;

    public int Count => _items.Count;
    public bool IsReadOnly => false;

    public T this[int index] {
        get => _items[index];
        set => Update((index, value), static (v, items) => items.SetItem(v.index, v.value));
    }

    public MutableList() : this(ImmutableList<T>.Empty) { }

    public override string ToString()
        => $"{GetType().GetName()}({Count} item(s))";

    public bool Update(ImmutableList<T> items)
    {
        lock (_lock) {
            if (_items == items)
                return false;

            _items = items;
            var oldWhenChangedSource = _whenChangedSource;
            _whenChangedSource = TaskCompletionSourceExt.New<Unit>();
            oldWhenChangedSource.TrySetResult(default);
        }
        Changed?.Invoke();
        return true;
    }

    public bool Update(ImmutableList<T> items, ImmutableList<T> expectedItems)
    {
        lock (_lock) {
            if (_items != expectedItems || _items == items)
                return false;

            _items = items;
            var oldWhenChangedSource = _whenChangedSource;
            _whenChangedSource = TaskCompletionSourceExt.New<Unit>();
            oldWhenChangedSource.TrySetResult(default);
        }
        Changed?.Invoke();
        return true;
    }

    public bool Update(Func<ImmutableList<T>, ImmutableList<T>> updater)
    {
        lock (_lock) {
            var items = _items;
            var newItems = updater.Invoke(items);
            if (newItems == items)
                return false;

            _items = newItems;
            var oldWhenChangedSource = _whenChangedSource;
            _whenChangedSource = TaskCompletionSourceExt.New<Unit>();
            oldWhenChangedSource.TrySetResult(default);
        }
        Changed?.Invoke();
        return true;
    }

    public bool Update<TState>(TState state, Func<TState, ImmutableList<T>, ImmutableList<T>> updater)
    {
        lock (_lock) {
            var items = _items;
            var newItems = updater.Invoke(state, items);
            if (newItems == items)
                return false;

            _items = newItems;
            var oldWhenChangedSource = _whenChangedSource;
            _whenChangedSource = TaskCompletionSourceExt.New<Unit>();
            oldWhenChangedSource.TrySetResult(default);
        }
        Changed?.Invoke();
        return true;
    }

    // IReadOnlyCollection members

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
    public IEnumerator<T> GetEnumerator()
        => Items.GetEnumerator();

    // IList members

    public void Add(T item)
        => Update(item, static (v, items) => items.Add(v));
    public bool Contains(T item)
        => Items.Contains(item);
    public void CopyTo(T[] array, int arrayIndex)
        => Items.CopyTo(array, arrayIndex);
    public bool Remove(T item)
        => Update(item, static (v, items) => items.Remove(v));
    public int IndexOf(T item)
        => Items.IndexOf(item);
    public void Insert(int index, T item)
        => Update((index, item), static (v, items) => items.Insert(v.index, v.item));
    public void RemoveAt(int index)
        => Update(index, static (i, items) => items.RemoveAt(i));
    public void Clear()
        => Update(ImmutableList<T>.Empty);
}
