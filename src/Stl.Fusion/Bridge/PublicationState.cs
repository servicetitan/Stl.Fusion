using System.Reactive;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Time;

namespace Stl.Fusion.Bridge
{
    public interface IPublicationState
    {
        IPublication Publication { get; }
        IntMoment CreatedAt { get; }
        IComputed Computed { get; }
        bool IsDisposed { get; }

        Task<object?> InvalidatedAsync();
        Task OutdatedAsync();
    }

    public interface IPublicationState<T> : IPublicationState
    {
        new IPublication<T> Publication { get; }
        new IComputed<T> Computed { get; }
    }

    public interface IPublicationStateImpl : IPublicationState
    {
        bool TryMarkOutdated();
    }

    public interface IPublicationStateImpl<T> : IPublicationStateImpl, IPublicationState<T> { }

    public class PublicationState<T> : IPublicationStateImpl<T>
    {
        protected readonly TaskSource<object?> InvalidatedSource;
        protected readonly TaskSource<Unit> OutdatedSource;

        IPublication IPublicationState.Publication => Publication;
        public IPublication<T> Publication { get; }
        IComputed IPublicationState.Computed => Computed;
        public IComputed<T> Computed { get; }
        public bool IsDisposed { get; }
        public IntMoment CreatedAt { get; }

        public PublicationState(IPublication<T> publication, IComputed<T> computed, bool isDisposed,
            TaskSource<object?> invalidatedSource = default,
            TaskSource<Unit> outdatedSource = default)
        {
            if (invalidatedSource.IsEmpty)
                invalidatedSource = TaskSource.New<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (outdatedSource.IsEmpty)
                outdatedSource = TaskSource.New<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);
            Publication = publication;
            CreatedAt = IntMoment.Now;
            IsDisposed = isDisposed;
            InvalidatedSource = invalidatedSource;
            OutdatedSource = outdatedSource;
            Computed = computed;
            computed.Invalidated += (_, invalidatedBy) => InvalidatedSource.TrySetResult(invalidatedBy);  
        }

        public Task<object?> InvalidatedAsync() => InvalidatedSource.Task;
        public Task OutdatedAsync() => OutdatedSource.Task;

        bool IPublicationStateImpl.TryMarkOutdated()
        {
            if (!OutdatedSource.TrySetResult(default))
                return false;
            InvalidatedSource.TrySetCanceled();
            return true;
        }
    }
}
