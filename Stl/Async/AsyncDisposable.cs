using System;
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
        public DisposalState DisposalState { get; private set; }

        public void Dispose()
        {
            // Completes when either DisposeAsync turns DisposeState to Disposing,
            // or if it's already in non-Active state.
            // The rest of disposal is supposed to be asynchronous.
#pragma warning disable 4014
            DisposeAsync(true);
#pragma warning restore 4014
        }

        public ValueTask DisposeAsync()
        {
            return DisposeAsync(true);
        }

        protected async ValueTask DisposeAsync(bool disposing)
        {
            if (DisposalState != DisposalState.Active)
                return;
            try {
                DisposalState = DisposalState.Disposing;
                await DisposeInternalAsync(disposing).ConfigureAwait(false);
            }
            finally {
                DisposalState = DisposalState.Disposed;
            }
        }
        
        protected virtual ValueTask DisposeInternalAsync(bool disposing) => 
            new ValueTask(Task.CompletedTask);
        
        protected void ThrowIfDisposedOrDisposing()
        {
            if (DisposalState != DisposalState.Active)
                throw Errors.ObjectDisposedOrDisposing();
        }
    }
}
