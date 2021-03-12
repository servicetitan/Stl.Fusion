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

        Task WhenInvalidated();
        Task WhenOutdated();
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
        protected readonly TaskSource<Unit> WhenInvalidatedSource;
        protected readonly TaskSource<Unit> WhenOutdatedSource;

        IPublication IPublicationState.Publication => Publication;
        public IPublication<T> Publication { get; }
        IComputed IPublicationState.Computed => Computed;
        public IComputed<T> Computed { get; }
        public bool IsDisposed { get; }
        public Moment CreatedAt { get; }

        public PublicationState(IPublication<T> publication, IComputed<T> computed, Moment createdAt, bool isDisposed,
            TaskSource<Unit> whenInvalidatedSource = default,
            TaskSource<Unit> whenOutdatedSource = default)
        {
            if (whenInvalidatedSource.IsEmpty)
                whenInvalidatedSource = TaskSource.New<Unit>(true);
            if (whenOutdatedSource.IsEmpty)
                whenOutdatedSource = TaskSource.New<Unit>(true);
            Publication = publication;
            CreatedAt = createdAt;
            IsDisposed = isDisposed;
            WhenInvalidatedSource = whenInvalidatedSource;
            WhenOutdatedSource = whenOutdatedSource;
            Computed = computed;
            computed.Invalidated += _ => WhenInvalidatedSource.TrySetResult(default);
        }

        public Task WhenInvalidated() => WhenInvalidatedSource.Task;
        public Task WhenOutdated() => WhenOutdatedSource.Task;

        bool IPublicationStateImpl.TryMarkOutdated()
        {
            if (!WhenOutdatedSource.TrySetResult(default))
                return false;
            // WhenInvalidatedSource result must be set
            // after setting WhenOutdatedSource result to
            // make sure the code awaiting for both events
            // can optimize for this case.
            WhenInvalidatedSource.TrySetResult(default);
            return true;
        }
    }
}
