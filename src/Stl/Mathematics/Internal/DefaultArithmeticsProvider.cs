using Stl.Extensibility;

namespace Stl.Mathematics.Internal;

public class DefaultArithmeticsProvider : ArithmeticsProvider
{
    private readonly ConcurrentDictionary<Type, IArithmetics> _cache = new();

    public IMatchingTypeFinder MatchingTypeFinder { get; init; } = new MatchingTypeFinder();

    public sealed override Arithmetics<T> GetArithmetics<T>()
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        => (Arithmetics<T>) _cache.GetOrAdd(typeof(T),
            static (type, self) => self.CreateArithmetics(type),
            this);

    protected virtual IArithmetics CreateArithmetics(Type type)
    {
        var arithmeticsType = MatchingTypeFinder.TryFind(type, typeof(IArithmetics));
        if (arithmeticsType == null)
            throw Errors.CantFindArithmetics(type);
        // ReSharper disable once HeapView.PossibleBoxingAllocation
        return (IArithmetics) arithmeticsType.CreateInstance();
    }
}
