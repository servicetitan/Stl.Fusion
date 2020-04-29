using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel.Internal
{
    [Serializable]
    public class ModelChangeSetDeserializationHelper
    {
        private Dictionary<Key, NodeChangeType>? _dictionary;
        [NonSerialized] 
        private volatile ImmutableDictionary<Key, NodeChangeType>? _immutableDictionary;

        public ModelChangeSetDeserializationHelper() { }
        public ModelChangeSetDeserializationHelper(Dictionary<Key, NodeChangeType> dictionary)
        {
            _dictionary = dictionary;
        }

        public ImmutableDictionary<Key, NodeChangeType>? GetImmutableDictionary()
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
