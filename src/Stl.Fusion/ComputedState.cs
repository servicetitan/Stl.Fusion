using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion
{
    public interface IComputedState : IState
    {
        public new interface IOptions : IState.IOptions { }

        Task WhenUpdatingAsync(CancellationToken cancellationToken = default);
        Task WhenUpdatedAsync(CancellationToken cancellationToken = default);
    }

    public interface IComputedState<T> : IState<T>, IComputedState { }

    public abstract class ComputedState<T> : State<T>, IComputedState<T>
    {
        private volatile Task<Unit> _updatingTask = null!;
        private volatile Task<Unit> _updatedTask = null!;

        public new class Options : State<T>.Options, IComputedState.IOptions { }

        public ComputedState(
            Options options, IServiceProvider serviceProvider,
            object? argument = null, bool initialize = true)
            : base(options, serviceProvider, argument, false)
        {
#pragma warning disable 420
            ReplaceRefTask(ref _updatingTask);
            ReplaceRefTask(ref _updatedTask);
#pragma warning restore 420
            if (initialize) Initialize(options);
        }

        public Task WhenUpdatingAsync(CancellationToken cancellationToken = default)
            => _updatingTask.WithFakeCancellation(cancellationToken);

        public Task WhenUpdatedAsync(CancellationToken cancellationToken = default)
            => _updatedTask.WithFakeCancellation(cancellationToken);


        protected override void OnUpdating()
        {
#pragma warning disable 420
            ReplaceRefTask(ref _updatingTask);
#pragma warning restore 420
            base.OnUpdating();
        }

        protected override void OnUpdated(IStateSnapshot<T>? oldSnapshot)
        {
#pragma warning disable 420
            ReplaceRefTask(ref _updatedTask);
#pragma warning restore 420
            base.OnUpdated(oldSnapshot);
        }

        private void ReplaceRefTask<TValue>(ref Task<TValue> task, TValue result = default)
        {
            var newTask = TaskSource.New<TValue>(true).Task;
            var oldTask = Interlocked.Exchange(ref task, newTask);
            if (oldTask != null!)
                TaskSource.For(oldTask).TrySetResult(result!);
        }
    }
}
