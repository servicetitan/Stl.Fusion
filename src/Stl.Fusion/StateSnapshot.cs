using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion
{
    public interface IStateSnapshot
    {
        IComputed Computed { get; }
        IComputed LatestNonErrorComputed { get; }
        int UpdateCount { get; }
        int ErrorCount { get; }
        int RetryCount { get; }

        Task WhenInvalidated(CancellationToken cancellationToken);
        Task WhenUpdating();
        Task WhenUpdated();
    }

    public interface IStateSnapshot<T> : IStateSnapshot
    {
        new IComputed<T> Computed { get; }
        new IComputed<T> LatestNonErrorComputed { get; }
    }

    public class StateSnapshot<T> : IStateSnapshot<T>
    {
        private TaskSource<Unit> WhenUpdatingSource { get; }
        private TaskSource<Unit> WhenUpdatedSource { get; }

        public IComputed<T> Computed { get; }
        public IComputed<T> LatestNonErrorComputed { get; }
        public int UpdateCount { get; }
        public int ErrorCount { get; }
        public int RetryCount { get; }

        // ReSharper disable once HeapView.PossibleBoxingAllocation
        IComputed IStateSnapshot.Computed => Computed;
        IComputed IStateSnapshot.LatestNonErrorComputed => LatestNonErrorComputed;

        public StateSnapshot(IComputed<T> computed)
        {
            Computed = computed;
            LatestNonErrorComputed = computed;
            WhenUpdatingSource = TaskSource.New<Unit>(true);
            WhenUpdatedSource = TaskSource.New<Unit>(true);
            UpdateCount = 0;
            ErrorCount = 0;
            RetryCount = 0;
        }

        public StateSnapshot(IComputed<T> computed, StateSnapshot<T> prevSnapshot)
        {
            Computed = computed;
            WhenUpdatingSource = TaskSource.New<Unit>(true);
            WhenUpdatedSource = TaskSource.New<Unit>(true);
            if (computed.HasValue) {
                LatestNonErrorComputed = computed;
                UpdateCount = 1 + prevSnapshot.UpdateCount;
                ErrorCount = prevSnapshot.ErrorCount;
                RetryCount = 0;
            }
            else {
                LatestNonErrorComputed = prevSnapshot.LatestNonErrorComputed;
                UpdateCount = 1 + prevSnapshot.UpdateCount;
                ErrorCount = 1 + prevSnapshot.ErrorCount;
                RetryCount = 1 + prevSnapshot.RetryCount;
            }
        }

        public override string ToString()
            => $"{GetType()}({Computed}, [{UpdateCount} update(s) / {ErrorCount} failure(s)])";

        public Task WhenInvalidated(CancellationToken cancellationToken)
            => Computed.WhenInvalidated(cancellationToken);
        public Task WhenUpdating() => WhenUpdatingSource.Task;
        public Task WhenUpdated() => WhenUpdatedSource.Task;

        protected internal void OnUpdating()
            => WhenUpdatingSource.TrySetResult(default);

        protected internal void OnUpdated()
        {
            WhenUpdatingSource.TrySetResult(default);
            WhenUpdatedSource.TrySetResult(default);
        }
    }
}
