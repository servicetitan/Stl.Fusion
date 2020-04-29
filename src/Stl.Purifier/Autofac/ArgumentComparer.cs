using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Stl.Purifier.Autofac
{
    public class ArgumentComparer : IEqualityComparer<object>
    {
        public static readonly ArgumentComparer Default = new ArgumentComparer();
        public static readonly IgnoreArgumentComparer Ignore = new IgnoreArgumentComparer();
        public static readonly ArgumentComparer ByRef = new ByRefArgumentComparer();

        public virtual int GetHashCode(object? obj) 
            => obj?.GetHashCode() ?? 0;
        
        public new virtual bool Equals(object objA, object objB)
        {
            if (objA == objB)
                return true;
            return objA?.Equals(objB) ?? false;
        }
    }

    public class IgnoreArgumentComparer : ArgumentComparer
    {
        public override int GetHashCode(object? obj) => 0;
        public override bool Equals(object objA, object objB) => true;
    }

    public class ByRefArgumentComparer : ArgumentComparer
    {
        public override int GetHashCode(object? obj) 
            => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
        public override bool Equals(object objA, object objB) 
            => ReferenceEquals(objA, objB);
    }
}
