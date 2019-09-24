using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    public interface ISimpleNode : INode, IReadOnlyDictionaryPlus<Symbol, object?>
    {
        ISimpleNode BaseWith(Symbol property, object? value);
        ISimpleNode BaseWith(IEnumerable<(Symbol PropertyKey, object? Value)> changes);
    }

    [Serializable]
    public abstract class SimpleNodeBase : ImmutableDictionaryNodeBase<Symbol, object?>, ISimpleNode
    {
        protected SimpleNodeBase(Key key) : base(key) { }
        protected SimpleNodeBase(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ISimpleNode BaseWith(Symbol property, object? value)
            => Update<SimpleNodeBase>(property, Option.Some(value));
        public ISimpleNode BaseWith(IEnumerable<(Symbol PropertyKey, object? Value)> changes)
            => Update<SimpleNodeBase>(changes.Select(p => (p.PropertyKey, Option.Some(p.Value))));
    }
}
