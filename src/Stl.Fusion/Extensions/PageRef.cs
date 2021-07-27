using Stl.Serialization;
using Stl.Text;

namespace Stl.Fusion.Extensions
{
    public record PageRef<TKey>(int Count, TKey? AfterKey = default)
    {
        public PageRef() : this(0) { }

        public override string ToString()
            => AfterKey == null
                ? Count.ToString()
                : SystemJsonSerializer.Default.Write(this, GetType());

        public static implicit operator PageRef<TKey>(int count)
            => new(count);
        public static implicit operator PageRef<TKey>((int Count, TKey? AfterKey) source)
            => new(source.Count, source.AfterKey);
    }

    public static class PageRef
    {
        public static readonly ListFormat ListFormat = ListFormat.CommaSeparated;

        public static PageRef<TKey> New<TKey>(int count, TKey? afterKey = default)
            => new(count, afterKey);

        public static PageRef<TKey> Parse<TKey>(string value)
            => int.TryParse(value, out var count)
                ? new PageRef<TKey>(count)
                : SystemJsonSerializer.Default.Reader.Read<PageRef<TKey>>(value);
    }
}
