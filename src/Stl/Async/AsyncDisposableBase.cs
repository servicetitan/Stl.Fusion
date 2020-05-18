using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public enum DisposalState
    {
        Active = 0,
        Disposing = 1,
        Disposed = 2,
    }

    public abstract class AsyncDisposableBase : IAsyncDisposable, IDisposable
    {
        private volatile int _disposalState;

        public DisposalState DisposalState => (DisposalState) _disposalState;

        public void Dispose()
        {
            // Completes when either DisposeAsync turns DisposeState to Disposing,
            // or if it's already in non-Active state.
            // The rest of disposal is supposed to be asynchronous.
            if (this is IAsyncDisposable ad)
                ad.DisposeAsync();
        }

        public ValueTask DisposeAsync()
        {
            return DisposeAsync(true);
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if ((int) DisposalState.Active != Interlocked.CompareExchange(
                ref _disposalState, 
                (int) DisposalState.Disposing,
                (int) DisposalState.Active))
                return;
            try {
                await DisposeInternalAsync(disposing).ConfigureAwait(false);
            }
            finally {
                Interlocked.Exchange(ref _disposalState, (int) DisposalState.Disposed);
            }
        }
        
        protected virtual ValueTask DisposeInternalAsync(bool disposing) => 
            new ValueTask(Task.CompletedTask);
        
        protected void ThrowIfDisposedOrDisposing()
        {
            if (DisposalState != DisposalState.Active)
                throw Errors.AlreadyDisposedOrDisposing();
        }
    }
}
