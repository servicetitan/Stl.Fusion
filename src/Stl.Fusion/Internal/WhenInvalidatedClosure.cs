using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion.Internal
{
    internal class WhenInvalidatedClosure
    {
        private readonly Action<IComputed> _onInvalidatedHandler;
        private readonly TaskSource<Unit> _taskSource;
        private readonly IComputed _computed;
        private readonly CancellationTokenRegistration _cancellationTokenRegistration;

        public Task Task => _taskSource.Task;

        internal WhenInvalidatedClosure(TaskSource<Unit> taskSource, IComputed computed, CancellationToken cancellationToken)
        {
            _taskSource = taskSource;
            _computed = computed;
            _onInvalidatedHandler = OnInvalidated;
            _computed.Invalidated += _onInvalidatedHandler;
            _cancellationTokenRegistration = cancellationToken.Register(OnUnregister);
        }

        private void OnInvalidated(IComputed _)
        {
            _taskSource.TrySetResult(default);
            _cancellationTokenRegistration.Dispose();
        }

        private void OnUnregister()
        {
            _taskSource.TrySetCanceled();
            _computed.Invalidated -= _onInvalidatedHandler;
        }
    }
}
