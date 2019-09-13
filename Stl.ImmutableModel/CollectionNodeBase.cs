using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    public interface ICollectionNode : INode, IReadOnlyDictionaryPlus<Key>
    {
        ICollectionNode BaseWith(Key key, Option<object?> item);
        ICollectionNode BaseWith(IEnumerable<(Key Key, Option<object?> Item)> changes);
    }
    
    public interface ICollectionNode<T> : ICollectionNode, IReadOnlyDictionaryPlus<Key, T>
    {
        ICollectionNode<T> BaseWith(Key key, Option<T> item);
        ICollectionNode<T> BaseWith(IEnumerable<(Key Key, Option<T> Item)> changes);
    }

    [Serializable]
    public abstract class CollectionNodeBase<T> : ImmutableDictionaryNodeBase<Key, T>, ICollectionNode<T>
    {
        protected CollectionNodeBase(Key key) : base(key) { }
        protected CollectionNodeBase(SerializationInfo info, StreamingContext context) : base(info, context) { }


        // Typed Update version
        public ICollectionNode<T> BaseWith(Key key, Option<T> item)
            => Update<CollectionNodeBase<T>>(key, item);
        public ICollectionNode<T> BaseWith(IEnumerable<(Key Key, Option<T> Item)> changes)
            => Update<CollectionNodeBase<T>>(changes);
        
        // Untyped Update version
        public ICollectionNode BaseWith(Key key, Option<object?> item)
            => BaseWith(key, item.Cast<T>());
        public ICollectionNode BaseWith(IEnumerable<(Key Key, Option<object?> Item)> changes)
            => BaseWith(changes.Select(p => (p.Key, p.Item.Cast<T>())));
    }
}
