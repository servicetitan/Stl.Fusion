using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    public interface ICollectionNode : INode, IReadOnlyDictionaryPlus<Symbol>
    {
        ICollectionNode BaseWith(Symbol localKey, Option<object?> item);
        ICollectionNode BaseWith(IEnumerable<(Symbol LocalKey, Option<object?> Item)> changes);
    }
    
    public interface ICollectionNode<T> : ICollectionNode, IReadOnlyDictionaryPlus<Symbol, T>
    {
        ICollectionNode<T> BaseWith(Symbol localKey, Option<T> item);
        ICollectionNode<T> BaseWith(IEnumerable<(Symbol LocalKey, Option<T> Item)> changes);
    }

    [Serializable]
    public abstract class CollectionNodeBase<T> : ImmutableDictionaryNodeBase<Symbol, T>, ICollectionNode<T>
    {
        protected CollectionNodeBase(Key key) : base(key) { }
        protected CollectionNodeBase(SerializationInfo info, StreamingContext context) : base(info, context) { }


        // Typed Update version
        public ICollectionNode<T> BaseWith(Symbol localKey, Option<T> item)
            => Update<CollectionNodeBase<T>>(localKey, item);
        public ICollectionNode<T> BaseWith(IEnumerable<(Symbol LocalKey, Option<T> Item)> changes)
            => Update<CollectionNodeBase<T>>(changes);
        
        // Untyped Update version
        public ICollectionNode BaseWith(Symbol localKey, Option<object?> item)
            => BaseWith(localKey, item.Cast<T>());
        public ICollectionNode BaseWith(IEnumerable<(Symbol LocalKey, Option<object?> Item)> changes)
            => BaseWith(changes.Select(p => (p.LocalKey, p.Item.Cast<T>())));
    }
}
