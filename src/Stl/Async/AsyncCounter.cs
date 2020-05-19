using System.Reactive;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public sealed class AsyncCounter : AsyncDisposableBase
    {
        private TaskCompletionStruct<Unit> _zeroTcs = TaskCompletionStruct<Unit>.Empty; 
        private readonly object _lock;
        private readonly TaskCreationOptions _taskCreationOptions;
        public int Count { get; private set; }
        
        public AsyncCounter(TaskCreationOptions taskCreationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
        {
            _taskCreationOptions = taskCreationOptions;
            _lock = this;
        }

        protected override ValueTask DisposeInternalAsync(bool disposing) 
            => WhenZeroAsync();

        public ValueTask WhenZeroAsync()
        {
            TaskCompletionStruct<Unit> zeroTcs;
            lock (_lock) {
                zeroTcs = _zeroTcs;
            }
            if (zeroTcs.IsEmpty)
                return ValueTaskEx.CompletedTask;
            return ((Task) zeroTcs.Task).ToValueTask();
        }

        public void Increment()
        {
            lock (_lock) {
                if (DisposalState != DisposalState.Active)
                    throw Errors.AlreadyDisposedOrDisposing(DisposalState);
                Count += 1;
                if (Count == 1)
                    _zeroTcs = new TaskCompletionStruct<Unit>(_taskCreationOptions);
            }
        }

        public void Decrement()
        {
            TaskCompletionStruct<Unit> zeroTcs = default;
            lock (_lock) {
                Count -= 1;
                if (Count == 0) {
                    zeroTcs = _zeroTcs;
                    _zeroTcs = default;
                }
            }
            if (zeroTcs.IsValid)
                zeroTcs.SetResult(default);
        }

        public Disposable<AsyncCounter> Use()
        {
            Increment();
            return Disposable.New(this, counter => counter.Decrement());
        }
    }
}
