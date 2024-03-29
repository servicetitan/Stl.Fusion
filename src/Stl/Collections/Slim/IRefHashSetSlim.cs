namespace Stl.Collections.Slim;

public interface IRefHashSetSlim<T>
    where T : class
{
    int Count { get; }
    IEnumerable<T> Items { get; }

    bool Contains(T item);
    bool Add(T item);
    bool Remove(T item);
    void Clear();

    void Apply<TState>(TState state, Action<TState, T> action);
    void Aggregate<TState>(ref TState state, Aggregator<TState, T> aggregator);
    TState Aggregate<TState>(TState state, Func<TState, T, TState> aggregator);
    void CopyTo(Span<T> target);
}
