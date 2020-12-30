using System;
using System.Threading;

namespace Stl.CommandR.Internal
{
    public interface ICommandContextImpl
    {
        void TrySetDefaultResult();
        void TrySetException(Exception exception);
        void TrySetCancelled(CancellationToken cancellationToken);
    }
}
