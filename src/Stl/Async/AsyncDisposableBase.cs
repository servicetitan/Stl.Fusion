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
        private volatile Task<Unit>? _disposeTcs = null;

        public DisposalState DisposalState {
            get {
                var disposeTcs = _disposeTcs;
                if (disposeTcs == null)
                    return DisposalState.Active;
                return disposeTcs.IsCompleted 
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

            var oldDisposeTcs = _disposeTcs;
            if (oldDisposeTcs != null) {
                await oldDisposeTcs.ConfigureAwait(false);
                return;
            }
            var disposeTcs = new TaskCompletionStruct<Unit>(TaskCreationOptions.None);
            oldDisposeTcs = Interlocked.CompareExchange(ref _disposeTcs, disposeTcs.Task, null); 
            if (oldDisposeTcs != null) {
                await oldDisposeTcs.ConfigureAwait(false);
                return;
            }
            try {
                await DisposeInternalAsync(disposing).ConfigureAwait(false);
            }
            catch {
                // DisposeAsync should never throw
            }
            finally {
                disposeTcs.TrySetResult(default);
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
