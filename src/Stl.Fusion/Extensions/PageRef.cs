namespace Stl.Fusion.Extensions
{
    public record PageRef<TKey>(int Count, Option<TKey> AfterKey = default)
    {
        public PageRef() : this(0) { }

        public static implicit operator PageRef<TKey>(int count)
            => new(count);
        public static implicit operator PageRef<TKey>((int Count, Option<TKey> AfterKey) source)
            => new(source.Count, source.AfterKey);
        public static implicit operator PageRef<TKey>((int Count, TKey AfterKey) source)
            => new(source.Count, source.AfterKey);
    }

    public static class PageRef
    {
        public static PageRef<TKey> New<TKey>(int count, Option<TKey> afterKey = default)
            => new(count, afterKey);
        public static PageRef<TKey> New<TKey>(int count, TKey afterKey)
            => new(count, afterKey);
    }
}
