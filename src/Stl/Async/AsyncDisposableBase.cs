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

    public interface IAsyncDisposableWithDisposalState : IAsyncDisposable, IDisposable
    {
        DisposalState DisposalState { get; }
    }

    public abstract class AsyncDisposableBase : IAsyncDisposableWithDisposalState
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
            DisposeAsync(true).Ignore();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true).ConfigureAwait(false);
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            GC.SuppressFinalize(this);

            // The logic is a bit complicated b/c we want any DisposeAsync
            // call to complete only when the actual dispose completes,
            // not earlier.

            var oldDisposeCompleted = _disposeCompleted;
            if (oldDisposeCompleted != null) {
                await oldDisposeCompleted.ConfigureAwait(false);
                return;
            }
            // Double-check CAS to save on Task creation
            var disposeCompletedSource = TaskSource.New<Unit>(TaskCreationOptions.None);
            oldDisposeCompleted = Interlocked.CompareExchange(
                ref _disposeCompleted, disposeCompletedSource.Task, null);
            if (oldDisposeCompleted != null) {
                await oldDisposeCompleted.ConfigureAwait(false);
                return;
            }
            try {
                await DisposeInternal(disposing).ConfigureAwait(false);
            }
            catch {
                // DisposeAsync should never throw
            }
            finally {
                disposeCompletedSource.TrySetResult(default);
            }
        }

        protected virtual ValueTask DisposeInternal(bool disposing)
            => ValueTaskEx.CompletedTask;

        protected bool MarkDisposed()
        {
            var success = null == Interlocked.CompareExchange(ref _disposeCompleted, TaskEx.UnitTask, null);
            if (success)
                GC.SuppressFinalize(this);
            return success;
        }

        protected void ThrowIfDisposedOrDisposing()
        {
            if (DisposalState != DisposalState.Active)
                throw Errors.AlreadyDisposedOrDisposing();
        }
    }
}
