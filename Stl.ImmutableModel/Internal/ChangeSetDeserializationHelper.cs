using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Stl.ImmutableModel.Internal
{
    [Serializable]
    public class ChangeSetDeserializationHelper
    {
        private Dictionary<DomainKey, ChangeKind>? _dictionary;
        [NonSerialized] 
        private volatile ImmutableDictionary<DomainKey, ChangeKind>? _immutableDictionary;

        public ChangeSetDeserializationHelper() { }
        public ChangeSetDeserializationHelper(Dictionary<DomainKey, ChangeKind> dictionary)
        {
            _dictionary = dictionary;
        }

        public ImmutableDictionary<DomainKey, ChangeKind>? GetImmutableDictionary()
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
