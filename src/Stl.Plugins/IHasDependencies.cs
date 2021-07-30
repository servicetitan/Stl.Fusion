using System;
using System.Collections.Generic;

namespace Stl.Plugins
{
    public interface IHasDependencies
    {
        IEnumerable<Type> Dependencies { get; }
    }
}