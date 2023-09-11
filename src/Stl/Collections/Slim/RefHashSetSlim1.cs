namespace Stl.Collections.Slim;

public struct RefHashSetSlim1<T> : IRefHashSetSlim<T>
    where T : class
{
    private T? _item;
    private HashSet<T>? _set;

    private readonly bool HasSet {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _set != null;
    }

    public readonly int Count {
        get {
            if (HasSet) return _set!.Count;
            if (_item == null) return 0;
            return 1;
        }
    }

    public readonly bool Contains(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (HasSet) return _set!.Contains(item);
        if (_item == item) return true;
        return false;
    }

    public bool Add(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (HasSet) return _set!.Add(item);

        // Item 1
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (_item == null) {
            _item = item;
            return true;
        }
        if (_item == item) return false;

        _set = new HashSet<T>(ReferenceEqualityComparer<T>.Instance) {
            _item, item
        };
        _item = default!;
        return true;
    }

    public bool Remove(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (HasSet) return _set!.Remove(item);

        // Item 1
        if (_item == null) return false;
        if (_item == item) {
            _item = default!;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _set = null;
        _item = default!;
    }

    public readonly IEnumerable<T> Items {
        get {
            if (HasSet) {
                foreach (var item in _set!)
                    yield return item;
                yield break;
            }
            if (_item == null) yield break;
            yield return _item;
        }
    }

    public readonly void Apply<TState>(TState state, Action<TState, T> action)
    {
        if (HasSet) {
            foreach (var item in _set!)
                action(state, item);
            return;
        }
        if (_item == null) return;
        action(state, _item);
    }


    public readonly void Aggregate<TState>(ref TState state, Aggregator<TState, T> aggregator)
    {
        if (HasSet) {
            foreach (var item in _set!)
                aggregator(ref state, item);
            return;
        }
        if (_item == null) return;
        aggregator(ref state, _item);
    }

    public readonly TState Aggregate<TState>(TState state, Func<TState, T, TState> aggregator)
    {
        if (HasSet) {
            foreach (var item in _set!)
                state = aggregator(state, item);
            return state;
        }
        if (_item == null) return state;
        state = aggregator(state, _item);
        return state;
    }

    public void CopyTo(Span<T> target)
    {
        var index = 0;
        if (HasSet) {
            foreach (var item in _set!)
                target[index++] = item;
            return;
        }
        if (_item == null) return;
        target[index] = _item;
    }
}
