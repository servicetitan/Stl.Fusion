using System;
using System.Threading;

namespace Stl.Fusion
{
    public interface IStateSnapshot
    {
        IComputed Computed { get; }
        IComputed LatestNonErrorComputed { get; }

        int UpdateCount { get; }
        int ErrorCount { get; }
        int RetryCount { get; }
        bool IsUpdating { get; set; }
    }

    public interface IStateSnapshot<T> : IStateSnapshot
    {
        new IComputed<T> Computed { get; }
        new IComputed<T> LatestNonErrorComputed { get; }
    }

    public class StateSnapshot<T> : IStateSnapshot<T>
    {
        private volatile int _isUpdating = 0;

        public bool IsUpdating {
            get => _isUpdating != 0;
            set {
                if (value == false)
                    throw new ArgumentOutOfRangeException(nameof(value));
                Interlocked.CompareExchange(ref _isUpdating, 1, 0);
            }
        }

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
            UpdateCount = 0;
            ErrorCount = 0;
            RetryCount = 0;
        }

        public StateSnapshot(IComputed<T> computed, StateSnapshot<T> lastSnapshot)
        {
            Computed = computed;
            if (computed.HasValue) {
                LatestNonErrorComputed = computed;
                UpdateCount = 1 + lastSnapshot.UpdateCount;
                ErrorCount = lastSnapshot.ErrorCount;
                RetryCount = 0;
            }
            else {
                LatestNonErrorComputed = lastSnapshot.LatestNonErrorComputed;
                UpdateCount = 1 + lastSnapshot.UpdateCount;
                ErrorCount = 1 + lastSnapshot.ErrorCount;
                RetryCount = 1 + lastSnapshot.RetryCount;
            }
        }

        public override string ToString()
            => $"{GetType()}({Computed}, [{UpdateCount} update(s) / {ErrorCount} failure(s)])";
    }
}
