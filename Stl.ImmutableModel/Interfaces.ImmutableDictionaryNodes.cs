using System.Collections.Immutable;

namespace Stl.ImmutableModel 
{
    public interface IImmutableDictionaryBasedSimpleNode : ISimpleNode
    {
        new ImmutableDictionary<Symbol, object?> Items { get; }
        ISimpleNode BaseWithout(Symbol property);
    }

    public interface IImmutableDictionaryBasedCollectionNode : ICollectionNode
    { }

    public interface IImmutableDictionaryBasedCollectionNode<T> : ICollectionNode<T>,
        IImmutableDictionaryBasedCollectionNode
    {
        new ImmutableDictionary<Symbol, T> Items { get; }
    }
}
