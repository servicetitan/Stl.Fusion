using System.Reactive;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Time;

namespace Stl.Fusion.Bridge
{
    public interface IPublicationState
    {
        IPublication Publication { get; }
        Moment CreatedAt { get; }
        IComputed Computed { get; }
        bool IsDisposed { get; }

        Task InvalidatedAsync();
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
        protected readonly TaskSource<Unit> InvalidatedSource;
        protected readonly TaskSource<Unit> OutdatedSource;

        IPublication IPublicationState.Publication => Publication;
        public IPublication<T> Publication { get; }
        IComputed IPublicationState.Computed => Computed;
        public IComputed<T> Computed { get; }
        public bool IsDisposed { get; }
        public Moment CreatedAt { get; }

        public PublicationState(IPublication<T> publication, IComputed<T> computed, Moment createdAt, bool isDisposed,
            TaskSource<Unit> invalidatedSource = default,
            TaskSource<Unit> outdatedSource = default)
        {
            if (invalidatedSource.IsEmpty)
                invalidatedSource = TaskSource.New<Unit>(true);
            if (outdatedSource.IsEmpty)
                outdatedSource = TaskSource.New<Unit>(true);
            Publication = publication;
            CreatedAt = createdAt;
            IsDisposed = isDisposed;
            InvalidatedSource = invalidatedSource;
            OutdatedSource = outdatedSource;
            Computed = computed;
            computed.Invalidated += _ => InvalidatedSource.TrySetResult(default);
        }

        public Task InvalidatedAsync() => InvalidatedSource.Task;
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
