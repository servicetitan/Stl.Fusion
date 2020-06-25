using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Stl.Collections;

namespace Stl.Internal
{
    [Serializable]
    public class ChangeTrackingDictionarySerializationSurrogate<TKey, TValue, TOwner> 
        : SerializationSurrogateBase<ChangeTrackingDictionary<TKey, TValue>, TOwner>
        where TKey : notnull
    {
        public Dictionary<TKey, TValue>? Base { get; set; } = null;
        public Dictionary<TKey, TValue>? Dictionary { get; set; } = null;
        public Dictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)>? Changes { get; set; } = null;

        public ChangeTrackingDictionarySerializationSurrogate(ChangeTrackingDictionary<TKey, TValue> source)
        {
            if (source.Base.Count != 0)
                Base = source.Base.ToDictionary();
            if (source.Dictionary.Count != 0)
                Dictionary = source.Dictionary.ToDictionary();
            if (source.Changes.Count != 0)
                Changes = source.Changes.ToDictionary();
        }

        protected override ChangeTrackingDictionary<TKey, TValue> ToActualObject() 
            => new ChangeTrackingDictionary<TKey, TValue>(
                Base?.ToImmutableDictionary() ?? ImmutableDictionary<TKey, TValue>.Empty,
                Dictionary?.ToImmutableDictionary() ?? ImmutableDictionary<TKey, TValue>.Empty,
                Changes?.ToImmutableDictionary() ?? ImmutableDictionary<TKey, (DictionaryEntryChangeType ChangeType, TValue Value)>.Empty);
    }
}