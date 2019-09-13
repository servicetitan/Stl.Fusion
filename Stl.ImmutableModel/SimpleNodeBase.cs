using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    public interface ISimpleNode : INode, IReadOnlyDictionaryPlus<Symbol, object?>
    {
        ISimpleNode BaseWith(Symbol key, object? value);
        ISimpleNode BaseWith(IEnumerable<(Symbol Key, object? Value)> changes);
    }

    [Serializable]
    public abstract class SimpleNodeBase : ImmutableDictionaryNodeBase<Symbol, object?>, ISimpleNode
    {
        protected SimpleNodeBase(Key key) : base(key) { }
        protected SimpleNodeBase(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ISimpleNode BaseWith(Symbol key, object? value)
            => Update<SimpleNodeBase>(key, Option.Some(value));
        public ISimpleNode BaseWith(IEnumerable<(Symbol Key, object? Value)> changes)
            => Update<SimpleNodeBase>(changes.Select(p => (p.Key, Option.Some(p.Value))));
    }
}
