#if NETSTANDARD2_0

using System;

namespace Stl.Net
{
    internal interface IArrayOwner<T> : IDisposable
    {
        T[] Array { get; }
    }
}

#endif
