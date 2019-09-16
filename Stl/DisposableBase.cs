using System;
using System.Threading;
using Stl.Internal;

namespace Stl 
{
    public abstract class DisposableBase : IDisposable
    {
        private volatile int _isDisposed;

        public bool IsDisposed => _isDisposed != 0;
        
        // Dispose pattern

        public void Dispose() => Dispose(true);

        // ReSharper disable once MemberCanBePrivate.Global -- might be called from finalizers 
        protected void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0)
                return;
            DisposeInternal(disposing);
        }
        
        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw Errors.AlreadyDisposed();
        }

        protected abstract void DisposeInternal(bool disposing);
    }
}
