using System.Collections.Generic;
using Stl.ImmutableModel.Reflection;

namespace Stl.ImmutableModel
{
    public interface INode : IFreezable
    {
        Key Key { get; }
        Symbol LocalKey { get; }
    }

    public interface ISimpleNode : INode, IReadOnlyDictionaryPlus<Symbol, object?>
    {
        // "With" is needed here, because it's used by updatable indexes 
        ISimpleNode BaseWith(Symbol property, object? value);
        ISimpleNode BaseWith(IEnumerable<(Symbol PropertyKey, object? Value)> changes);
    }

    public interface ICollectionNode : INode, IReadOnlyDictionaryPlus<Symbol>
    {
        ICollectionNode BaseWith(Symbol localKey, Option<object?> item);
        ICollectionNode BaseWith(IEnumerable<(Symbol LocalKey, Option<object?> Item)> changes);
        ICollectionNode BaseWithCleared();
    }
    
    public interface ICollectionNode<T> : ICollectionNode, IReadOnlyDictionaryPlus<Symbol, T>
    {
        ICollectionNode<T> BaseWith(Symbol localKey, Option<T> item);
        ICollectionNode<T> BaseWith(IEnumerable<(Symbol LocalKey, Option<T> Item)> changes);
    }
}
