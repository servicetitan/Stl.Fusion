using System.Collections.Generic;

namespace Stl.Comparison
{
    public class HasIdEqualityComparer<T> : IEqualityComparer<IHasId<T>>
    {
        public static readonly IEqualityComparer<IHasId<T>> Instance = 
            new HasIdEqualityComparer<T>();

        public bool Equals(IHasId<T>? x, IHasId<T>? y)
        {
            if (x == null)
                return y == null;
            return y != null && EqualityComparer<T>.Default.Equals(x.Id, y.Id);
        }

        public int GetHashCode(IHasId<T>? obj) 
            => obj == null ? 0 : EqualityComparer<T>.Default.GetHashCode(obj.Id);
    }
}
