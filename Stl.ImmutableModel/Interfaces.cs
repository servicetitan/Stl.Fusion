using System.Collections.Generic;
using Stl.Extensibility;
using Stl.ImmutableModel.Indexing;
using Stl.Text;

namespace Stl.ImmutableModel
{
    public interface INode : IFreezable, IHasChangeHistory
    {
        Key Key { get; set; }
    }

    public interface ISimpleNode : INode, IHasOptions
    {}

    public interface ICollectionNode : INode
    {
        IEnumerable<Key> Keys { get; }
        IEnumerable<object?> Values { get; }
        IEnumerable<KeyValuePair<Key, object?>> Items { get; }
        
        object? this[Key key] { get; set; }
        bool ContainsKey(Key key);
        bool TryGetValue(Key key, out object? value);
        void Add(Key key, object? value);
        bool Remove(Key key);
        void Clear();
    }
    
    public interface ICollectionNode<T> : ICollectionNode, IDictionary<Key, T>, IHasChangeHistory<T>
    {}
}
