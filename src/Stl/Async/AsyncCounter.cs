using System.Reactive;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public sealed class AsyncCounter : AsyncDisposableBase
    {
        private TaskSource<Unit> _zeroSource = TaskSource<Unit>.Empty;
        private readonly object _lock;
        private readonly TaskCreationOptions _taskCreationOptions;
        public int Count { get; private set; }

        public AsyncCounter(TaskCreationOptions taskCreationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
        {
            _taskCreationOptions = taskCreationOptions;
            _lock = this;
        }

        protected override ValueTask DisposeInternal(bool disposing)
            => WhenZero();

        public ValueTask WhenZero()
        {
            TaskSource<Unit> zeroSource;
            lock (_lock) {
                zeroSource = _zeroSource;
            }
            if (zeroSource.IsEmpty)
                return ValueTaskEx.CompletedTask;
            return ((Task) zeroSource.Task).ToValueTask();
        }

        public void Increment()
        {
            lock (_lock) {
                if (DisposalState != DisposalState.Active)
                    throw Errors.AlreadyDisposedOrDisposing(DisposalState);
                Count += 1;
                if (Count == 1)
                    _zeroSource = TaskSource.New<Unit>(_taskCreationOptions);
            }
        }

        public void Decrement()
        {
            TaskSource<Unit> zeroSource = default;
            lock (_lock) {
                Count -= 1;
                if (Count == 0) {
                    zeroSource = _zeroSource;
                    _zeroSource = default;
                }
            }
            if (!zeroSource.IsEmpty)
                zeroSource.SetResult(default);
        }

        public Disposable<AsyncCounter> Use()
        {
            Increment();
            return Disposable.New(this, counter => counter.Decrement());
        }
    }
}
