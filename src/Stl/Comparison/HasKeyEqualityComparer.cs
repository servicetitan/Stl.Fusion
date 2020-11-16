using System.Collections.Generic;

namespace Stl.Comparison
{
    public class HasKeyEqualityComparer<T> : IEqualityComparer<IHasKey<T>>
    {
        public static readonly IEqualityComparer<IHasKey<T>> Instance =
            new HasKeyEqualityComparer<T>();

        public bool Equals(IHasKey<T>? x, IHasKey<T>? y)
        {
            if (x == null)
                return y == null;
            return y != null && EqualityComparer<T>.Default.Equals(x.Key, y.Key);
        }

        public int GetHashCode(IHasKey<T>? obj)
            => obj == null ? 0 : EqualityComparer<T>.Default.GetHashCode(obj.Key!);
    }
}
