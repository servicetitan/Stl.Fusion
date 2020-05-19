using System;
using System.Reactive;
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
        private volatile Task<Unit>? _disposeCompleted = null;

        public DisposalState DisposalState {
            get {
                var disposeCompleted = _disposeCompleted;
                if (disposeCompleted == null)
                    return DisposalState.Active;
                return disposeCompleted.IsCompleted 
                    ? DisposalState.Disposed 
                    : DisposalState.Disposing;
            }
        }

        public void Dispose()
        {
            // Completes when either DisposeAsync turns DisposeState to Disposing,
            // or if it's already in non-Active state.
            // The rest of disposal is supposed to be asynchronous.
            if (this is IAsyncDisposable ad)
                ad.DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true).ConfigureAwait(false);
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            // The logic is a bit complicated b/c we want any DisposeAsync
            // call to complete only when the actual dispose completes,
            // not earlier.

            var oldDisposeCompleted = _disposeCompleted;
            if (oldDisposeCompleted != null) {
                await oldDisposeCompleted.ConfigureAwait(false);
                return;
            }
            var disposeCompletedSource = TaskSource.New<Unit>(TaskCreationOptions.None);
            oldDisposeCompleted = Interlocked.CompareExchange(
                ref _disposeCompleted, disposeCompletedSource.Task, null); 
            if (oldDisposeCompleted != null) {
                await oldDisposeCompleted.ConfigureAwait(false);
                return;
            }
            try {
                await DisposeInternalAsync(disposing).ConfigureAwait(false);
            }
            catch {
                // DisposeAsync should never throw
            }
            finally {
                disposeCompletedSource.TrySetResult(default);
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
