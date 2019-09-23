using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    public interface ICollectionNode : INode, IReadOnlyDictionaryPlus<LocalKey>
    {
        ICollectionNode BaseWith(LocalKey localKey, Option<object?> item);
        ICollectionNode BaseWith(IEnumerable<(LocalKey LocalKey, Option<object?> Item)> changes);
    }
    
    public interface ICollectionNode<T> : ICollectionNode, IReadOnlyDictionaryPlus<LocalKey, T>
    {
        ICollectionNode<T> BaseWith(LocalKey localKey, Option<T> item);
        ICollectionNode<T> BaseWith(IEnumerable<(LocalKey LocalKey, Option<T> Item)> changes);
    }

    [Serializable]
    public abstract class CollectionNodeBase<T> : ImmutableDictionaryNodeBase<LocalKey, T>, ICollectionNode<T>
    {
        protected CollectionNodeBase(Key key) : base(key) { }
        protected CollectionNodeBase(SerializationInfo info, StreamingContext context) : base(info, context) { }


        // Typed Update version
        public ICollectionNode<T> BaseWith(LocalKey localKey, Option<T> item)
            => Update<CollectionNodeBase<T>>(localKey, item);
        public ICollectionNode<T> BaseWith(IEnumerable<(LocalKey LocalKey, Option<T> Item)> changes)
            => Update<CollectionNodeBase<T>>(changes);
        
        // Untyped Update version
        public ICollectionNode BaseWith(LocalKey localKey, Option<object?> item)
            => BaseWith(localKey, item.Cast<T>());
        public ICollectionNode BaseWith(IEnumerable<(LocalKey LocalKey, Option<object?> Item)> changes)
            => BaseWith(changes.Select(p => (p.LocalKey, p.Item.Cast<T>())));
    }
}
