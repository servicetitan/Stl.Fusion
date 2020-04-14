using System.Reactive;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public sealed class AsyncCounter : AsyncDisposableBase
    {
        private TaskCompletionSource<Unit>? _zeroTcs = null; 
        private readonly object _lock;
        public int Count { get; private set; }
        
        public AsyncCounter()
        {
            _lock = this;
        }

        protected override ValueTask DisposeInternalAsync(bool disposing) 
            => WhenZeroAsync();

        public ValueTask WhenZeroAsync()
        {
            var zeroTcs = (TaskCompletionSource<Unit>?) null;
            lock (_lock) {
                zeroTcs = _zeroTcs;
            }
            return ((Task?) zeroTcs?.Task)?.ToValueTask() ?? ValueTaskEx.CompletedTask;
        }

        public void Increment()
        {
            lock (_lock) {
                if (DisposalState != DisposalState.Active)
                    throw Errors.AlreadyDisposedOrDisposing(DisposalState);
                Count += 1;
                if (Count == 1)
                    _zeroTcs = new TaskCompletionSource<Unit>();
            }
        }

        public void Decrement()
        {
            var zeroTcs = (TaskCompletionSource<Unit>?) null;
            lock (_lock) {
                Count -= 1;
                if (Count == 0) {
                    zeroTcs = _zeroTcs;
                    _zeroTcs = null;
                }
            }
            zeroTcs?.SetResult(default);
        }

        public Disposable<AsyncCounter> Use()
        {
            Increment();
            return Disposable.New(this, counter => counter.Decrement());
        }
    }
}
