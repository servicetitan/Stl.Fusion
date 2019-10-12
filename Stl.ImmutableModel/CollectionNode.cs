using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    [Serializable]
    public class CollectionNode<T> : ImmutableDictionaryNodeBase<Symbol, T>, 
        IImmutableDictionaryBasedCollectionNode<T>
    {
        public CollectionNode(Key key) : base(key) { }
        public CollectionNode(SerializationInfo info, StreamingContext context) : base(info, context) { }

        // Typed Update version
        public ICollectionNode<T> BaseWith(Symbol localKey, Option<T> item)
            => BaseWith<CollectionNode<T>>(localKey, item);
        public ICollectionNode<T> BaseWith(IEnumerable<(Symbol LocalKey, Option<T> Item)> changes)
            => BaseWith<CollectionNode<T>>(changes);
        
        // Untyped Update version
        public ICollectionNode BaseWith(Symbol localKey, Option<object?> item)
            => BaseWith(localKey, item.Cast<T>());
        public ICollectionNode BaseWith(IEnumerable<(Symbol LocalKey, Option<object?> Item)> changes)
            => BaseWith(changes.Select(p => (p.LocalKey, p.Item.Cast<T>())));
    }
}
