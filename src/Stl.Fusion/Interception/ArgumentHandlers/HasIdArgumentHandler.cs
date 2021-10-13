using System.Collections.Generic;
using Stl.Extensibility;

namespace Stl.Fusion.Interception
{
    [MatchFor(typeof(IHasId<>), typeof(ArgumentHandlerProvider))]
    public class HasIdArgumentHandler<T> : ArgumentHandler
    {
        public static bool IsClass { get; } = typeof(T).IsClass;
        public static HasIdArgumentHandler<T> Instance { get; } = new();

        private HasIdArgumentHandler()
        {
            GetHashCodeFunc = obj => {
                var hasId = (IHasId<T>?) obj;
                return hasId == null ? 0 : EqualityComparer<T>.Default.GetHashCode(hasId.Id!);
            };
            EqualsFunc = (a, b) => {
                if (IsClass && ReferenceEquals(a, b))
                    return true;
                if (a == null)
                    return b == null;
                if (b == null)
                    return false;
                var aHasId = (IHasId<T>) a;
                var bHasId = (IHasId<T>) b;
                return EqualityComparer<T>.Default.Equals(aHasId.Id, bHasId.Id);
            };
        }
    }
}
