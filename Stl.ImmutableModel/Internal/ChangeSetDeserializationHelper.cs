using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel.Internal
{
    [Serializable]
    public class ChangeSetDeserializationHelper
    {
        private Dictionary<DomainKey, NodeChangeType>? _dictionary;
        [NonSerialized] 
        private volatile ImmutableDictionary<DomainKey, NodeChangeType>? _immutableDictionary;

        public ChangeSetDeserializationHelper() { }
        public ChangeSetDeserializationHelper(Dictionary<DomainKey, NodeChangeType> dictionary)
        {
            _dictionary = dictionary;
        }

        public ImmutableDictionary<DomainKey, NodeChangeType>? GetImmutableDictionary()
        {
            if (_immutableDictionary != null) return _immutableDictionary;
            lock (this) {
                if (_immutableDictionary != null) return _immutableDictionary;
                _immutableDictionary = _dictionary.ToImmutableDictionary();
                _dictionary = null;
            }
            return _immutableDictionary;
        }
    }
}
