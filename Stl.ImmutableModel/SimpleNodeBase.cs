using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Stl.ImmutableModel 
{
    [Serializable]
    public abstract class SimpleNodeBase : ImmutableDictionaryNodeBase<Symbol, object?>, 
        IExtendableNode
    {
        protected SimpleNodeBase(Key key) : base(key) { }
        protected SimpleNodeBase(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ISimpleNode BaseWith(Symbol property, object? value)
            => BaseWith<SimpleNodeBase>(property, Option.Some(value));
        public ISimpleNode BaseWith(IEnumerable<(Symbol PropertyKey, object? Value)> changes)
            => BaseWith<SimpleNodeBase>(changes.Select(p => (p.PropertyKey, Option.Some(p.Value))));
        public ISimpleNode BaseWithout(Symbol property) 
            => BaseWith<SimpleNodeBase>(property, Option.None<object?>());

        public IExtendableNode BaseWithExt(Symbol extension, object? value) 
            => BaseWith<SimpleNodeBase>(extension,
                // Explicit type spec. is needed to suppress nullability diff. warning
                value != null ? Option.Some<object?>(value) : default);
        public IExtendableNode BaseWithAllExt(IEnumerable<(Symbol Extension, object? Value)> extensions)
            => BaseWith<SimpleNodeBase>(extensions.Select(p => 
                // Explicit type spec. is needed to suppress nullability diff. warning
                (p.Extension, p.Value != null ? Option.Some<object?>(p.Value) : default)!)); 
    }
}
