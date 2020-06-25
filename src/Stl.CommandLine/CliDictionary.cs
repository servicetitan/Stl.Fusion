using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace Stl.CommandLine 
{
    [Serializable]
    public class CliDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IEnumerable<IFormattable>
        where TKey : notnull
    {
        public static readonly string DefaultItemTemplate = "{0}={1}";
        public string ItemTemplate { get; set; } = DefaultItemTemplate;

        public CliDictionary() { }
        public CliDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
        public CliDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer) { }
        public CliDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection) { }
        public CliDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer) { }
        public CliDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
        public CliDictionary(int capacity) : base(capacity) { }
        public CliDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer) { }
        protected CliDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public new IEnumerator<IFormattable> GetEnumerator()
            => (this as IEnumerable<KeyValuePair<TKey, TValue>>)
                // ReSharper disable once HeapView.BoxingAllocation
                .Select(x => CliString.New(
                    string.Format(CultureInfo.InvariantCulture, ItemTemplate, x.Key, x.Value)
                    ) as IFormattable)
                .GetEnumerator();
    }
}
