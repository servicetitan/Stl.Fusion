using System.Collections.Generic;

namespace Stl.Frozen
{
    public interface IFrozenCollection<T> : ICollection<T>, IFrozenEnumerable<T> { }
}
