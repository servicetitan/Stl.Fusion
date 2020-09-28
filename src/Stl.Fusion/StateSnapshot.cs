using System;
using System.Threading;

namespace Stl.Fusion
{
    public interface IStateSnapshot
    {
        IComputed Computed { get; }
        IComputed LastValueComputed { get; }
        object? LastValue { get; }
        int UpdateCount { get; }
        int FailureCount { get; }
        int RetryCount { get; }
        bool IsUpdating { get; set; }
    }

    public interface IStateSnapshot<T> : IStateSnapshot
    {
        new IComputed<T> Computed { get; }
        new IComputed<T> LastValueComputed { get; }
        new T LastValue { get; }
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
        public IComputed<T> LastValueComputed { get; }
        public T LastValue => LastValueComputed.Value;
        public int UpdateCount { get; }
        public int FailureCount { get; }
        public int RetryCount { get; }

        // ReSharper disable once HeapView.PossibleBoxingAllocation
        object? IStateSnapshot.LastValue => LastValue;
        IComputed IStateSnapshot.Computed => Computed;
        IComputed IStateSnapshot.LastValueComputed => LastValueComputed;

        public StateSnapshot(IComputed<T> computed)
        {
            Computed = computed;
            LastValueComputed = computed;
            UpdateCount = 0;
            FailureCount = 0;
            RetryCount = 0;
        }

        public StateSnapshot(IComputed<T> computed, StateSnapshot<T> lastSnapshot)
        {
            Computed = computed;
            if (computed.HasValue) {
                LastValueComputed = computed;
                UpdateCount = 1 + lastSnapshot.UpdateCount;
                FailureCount = lastSnapshot.FailureCount;
                RetryCount = 0;
            }
            else {
                LastValueComputed = lastSnapshot.LastValueComputed;
                UpdateCount = 1 + lastSnapshot.UpdateCount;
                FailureCount = 1 + lastSnapshot.FailureCount;
                RetryCount = 1 + lastSnapshot.RetryCount;
            }
        }

        public override string ToString()
            => $"{GetType()}({Computed}, [{UpdateCount} update(s) / {FailureCount} failure(s)])";
    }
}
