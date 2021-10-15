namespace Stl.Collections.Slim;

public struct HashSetSlim3<T> : IHashSetSlim<T>
    where T : notnull
{
    private int _count;
    private (T, T, T) _tuple;
    private HashSet<T>? _set;

    private bool HasSet {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _set != null;
    }

    public int Count {
        get {
            if (HasSet) return _set!.Count;
            return _count;
        }
    }

    public bool Contains(T item)
    {
        if (HasSet) return _set!.Contains(item);
        if (_count >= 1 && EqualityComparer<T>.Default.Equals(_tuple.Item1, item)) return true;
        if (_count >= 2 && EqualityComparer<T>.Default.Equals(_tuple.Item2, item)) return true;
        if (_count >= 3 && EqualityComparer<T>.Default.Equals(_tuple.Item3, item)) return true;
        return false;
    }

    public bool Add(T item)
    {
        if (HasSet) return _set!.Add(item);

        // Item 1
        if (_count < 1) {
            _tuple.Item1 = item;
            _count++;
            return true;
        }
        if (EqualityComparer<T>.Default.Equals(_tuple.Item1, item)) return true;

        // Item 2
        if (_count < 2) {
            _tuple.Item2 = item;
            _count++;
            return true;
        }
        if (EqualityComparer<T>.Default.Equals(_tuple.Item2, item)) return true;

        // Item 3
        if (_count < 3) {
            _tuple.Item3 = item;
            _count++;
            return true;
        }
        if (EqualityComparer<T>.Default.Equals(_tuple.Item3, item)) return true;

        _set = new HashSet<T> {
            _tuple.Item1, _tuple.Item2, _tuple.Item3, item
        };
        _tuple = default;
        return true;
    }

    public bool Remove(T item)
    {
        if (HasSet) return _set!.Remove(item);

        // Item 1
        if (_count < 1) return false;
        if (EqualityComparer<T>.Default.Equals(_tuple.Item1, item)) {
            _tuple = (_tuple.Item2, _tuple.Item3, default)!;
            _count--;
            return true;
        }

        // Item 2
        if (_count < 2) return false;
        if (EqualityComparer<T>.Default.Equals(_tuple.Item2, item)) {
            _tuple = (_tuple.Item1, _tuple.Item3, default)!;
            _count--;
            return true;
        }

        // Item 3
        if (_count < 3) return false;
        if (EqualityComparer<T>.Default.Equals(_tuple.Item3, item)) {
            _tuple = (_tuple.Item1, _tuple.Item2, default)!;
            _count--;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _set = null;
        _tuple = default;
        _count = 0;
    }

    public IEnumerable<T> Items {
        get {
            if (HasSet) {
                foreach (var item in _set!)
                    yield return item;
                yield break;
            }
            if (_count < 1) yield break;
            yield return _tuple.Item1;
            if (_count < 2) yield break;
            yield return _tuple.Item2;
            if (_count < 3) yield break;
            yield return _tuple.Item3;
        }
    }

    public void Apply<TState>(TState state, Action<TState, T> action)
    {
        if (HasSet) {
            foreach (var item in _set!)
                action(state, item);
            return;
        }
        if (_count < 1) return;
        action(state, _tuple.Item1);
        if (_count < 2) return;
        action(state, _tuple.Item2);
        if (_count < 3) return;
        action(state, _tuple.Item3);
    }

    public void Aggregate<TState>(ref TState state, Aggregator<TState, T> aggregator)
    {
        if (HasSet) {
            foreach (var item in _set!)
                aggregator(ref state, item);
            return;
        }
        if (_count < 1) return;
        aggregator(ref state, _tuple.Item1);
        if (_count < 2) return;
        aggregator(ref state, _tuple.Item2);
        if (_count < 3) return;
        aggregator(ref state, _tuple.Item3);
    }

    public TState Aggregate<TState>(TState state, Func<TState, T, TState> aggregator)
    {
        if (HasSet) {
            foreach (var item in _set!)
                state = aggregator(state, item);
            return state;
        }
        if (_count < 1) return state;
        state = aggregator(state, _tuple.Item1);
        if (_count < 2) return state;
        state = aggregator(state, _tuple.Item2);
        if (_count < 3) return state;
        state = aggregator(state, _tuple.Item3);
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
        if (_count < 1) return;
        target[index++] = _tuple.Item1;
        if (_count < 2) return;
        target[index++] = _tuple.Item2;
        if (_count < 3) return;
        target[index] = _tuple.Item3;
    }
}
