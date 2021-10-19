namespace Stl.Collections.Slim;

public struct RefHashSetSlim4<T> : IRefHashSetSlim<T>
    where T : class
{
    private (T?, T?, T?, T?) _tuple;
    private HashSet<T>? _set;

    private bool HasSet {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _set != null;
    }

    public int Count {
        get {
            if (HasSet) return _set!.Count;
            if (_tuple.Item1 == null) return 0;
            if (_tuple.Item2 == null) return 1;
            if (_tuple.Item3 == null) return 2;
            if (_tuple.Item4 == null) return 3;
            return 4;
        }
    }

    public bool Contains(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (HasSet) return _set!.Contains(item);
        if (_tuple.Item1 == item) return true;
        if (_tuple.Item2 == item) return true;
        if (_tuple.Item3 == item) return true;
        if (_tuple.Item4 == item) return true;
        return false;
    }

    public bool Add(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (HasSet) return _set!.Add(item);

        // Item 1
        if (_tuple.Item1 == null) {
            _tuple.Item1 = item;
            return true;
        }
        if (_tuple.Item1 == item) return false;

        // Item 2
        if (_tuple.Item2 == null) {
            _tuple.Item2 = item;
            return true;
        }
        if (_tuple.Item2 == item) return false;

        // Item 3
        if (_tuple.Item3 == null) {
            _tuple.Item3 = item;
            return true;
        }
        if (_tuple.Item3 == item) return false;

        // Item 4
        if (_tuple.Item4 == null) {
            _tuple.Item4 = item;
            return true;
        }
        if (_tuple.Item4 == item) return false;

        _set = new HashSet<T>(ReferenceEqualityComparer<T>.Instance) {
            _tuple.Item1, _tuple.Item2, _tuple.Item3, _tuple.Item4, item
        };
        _tuple = default;
        return true;
    }

    public bool Remove(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (HasSet) return _set!.Remove(item);

        // Item 1
        if (_tuple.Item1 == null) return false;
        if (_tuple.Item1 == item) {
            _tuple = (_tuple.Item2, _tuple.Item3, _tuple.Item4, default!)!;
            return true;
        }

        // Item 2
        if (_tuple.Item2 == null) return false;
        if (_tuple.Item2 == item) {
            _tuple = (_tuple.Item1, _tuple.Item3, _tuple.Item4, default!)!;
            return true;
        }

        // Item 3
        if (_tuple.Item3 == null) return false;
        if (_tuple.Item3 == item) {
            _tuple = (_tuple.Item1, _tuple.Item2, _tuple.Item4, default!)!;
            return true;
        }

        // Item 4
        if (_tuple.Item4 == null) return false;
        if (_tuple.Item4 == item) {
            _tuple = (_tuple.Item1, _tuple.Item2, _tuple.Item3, default!)!;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _set = null;
        _tuple = default;
    }

    public IEnumerable<T> Items {
        get {
            if (HasSet) {
                foreach (var item in _set!)
                    yield return item;
                yield break;
            }
            if (_tuple.Item1 == null) yield break;
            yield return _tuple.Item1;
            if (_tuple.Item2 == null) yield break;
            yield return _tuple.Item2;
            if (_tuple.Item3 == null) yield break;
            yield return _tuple.Item3;
            if (_tuple.Item4 == null) yield break;
            yield return _tuple.Item4;
        }
    }

    public void Apply<TState>(TState state, Action<TState, T> action)
    {
        if (HasSet) {
            foreach (var item in _set!)
                action(state, item);
            return;
        }
        if (_tuple.Item1 == null) return;
        action(state, _tuple.Item1);
        if (_tuple.Item2 == null) return;
        action(state, _tuple.Item2);
        if (_tuple.Item3 == null) return;
        action(state, _tuple.Item3);
        if (_tuple.Item4 == null) return;
        action(state, _tuple.Item4);
    }

    public void Aggregate<TState>(ref TState state, Aggregator<TState, T> aggregator)
    {
        if (HasSet) {
            foreach (var item in _set!)
                aggregator(ref state, item);
            return;
        }
        if (_tuple.Item1 == null) return;
        aggregator(ref state, _tuple.Item1);
        if (_tuple.Item2 == null) return;
        aggregator(ref state, _tuple.Item2);
        if (_tuple.Item3 == null) return;
        aggregator(ref state, _tuple.Item3);
        if (_tuple.Item4 == null) return;
        aggregator(ref state, _tuple.Item4);
    }

    public TState Aggregate<TState>(TState state, Func<TState, T, TState> aggregator)
    {
        if (HasSet) {
            foreach (var item in _set!)
                state = aggregator(state, item);
            return state;
        }
        if (_tuple.Item1 == null) return state;
        state = aggregator(state, _tuple.Item1);
        if (_tuple.Item2 == null) return state;
        state = aggregator(state, _tuple.Item2);
        if (_tuple.Item3 == null) return state;
        state = aggregator(state, _tuple.Item3);
        if (_tuple.Item4 == null) return state;
        state = aggregator(state, _tuple.Item4);
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
        if (_tuple.Item1 == null) return;
        target[index++] = _tuple.Item1;
        if (_tuple.Item2 == null) return;
        target[index++] = _tuple.Item2;
        if (_tuple.Item3 == null) return;
        target[index++] = _tuple.Item3;
        if (_tuple.Item4 == null) return;
        target[index] = _tuple.Item4;
    }
}
