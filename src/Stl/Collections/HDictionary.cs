using System;
using System.Text;

namespace Stl.Collections
{
    public class HDictionary<TKey, TItem> : LazyDictionary<TKey, TItem>
        where TKey : notnull
        where TItem : HDictionary<TKey, TItem>
    {
        public TKey Key { get; }

        public HDictionary(TKey key) => Key = key;

        public string ToString(bool dump) => dump ? Dump(new StringBuilder()).ToString() : ToString();
        public override string ToString() => $"{Key} -> [{Count}]";

        public virtual StringBuilder Dump(StringBuilder sb, string indent = "")
        {
            sb.AppendLine($"{indent}{ToString()}");
            foreach (var sg in Values)
                sg.Dump(sb, "  " + indent);
            return sb;
        }

        public TItem GetOrAdd(TKey key, Func<TKey, TItem> factory)
        {
            var item = this.TryGetOrDefault(key, null!);
            if (item == null) {
                item = factory.Invoke(key);
                Add(key, item);
            }
            return item;
        }
    }
}
