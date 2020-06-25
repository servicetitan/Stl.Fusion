using System.Collections.Generic;

namespace Stl.Frozen
{
    public interface IFrozenEnumerable<out T> : IEnumerable<T>, IFrozen { }
}
