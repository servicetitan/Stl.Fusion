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
        protected readonly TaskCompletionStruct<object?> InvalidatedTcs;
        protected readonly TaskCompletionStruct<Unit> OutdatedTcs;

        IPublication IPublicationState.Publication => Publication;
        public IPublication<T> Publication { get; }
        IComputed IPublicationState.Computed => Computed;
        public IComputed<T> Computed { get; }
        public bool IsDisposed { get; }
        public IntMoment CreatedAt { get; }

        public PublicationState(IPublication<T> publication, IComputed<T> computed, bool isDisposed,
            TaskCompletionStruct<object?> invalidatedTcs = default,
            TaskCompletionStruct<Unit> outdatedTcs = default)
        {
            if (invalidatedTcs.IsEmpty)
                invalidatedTcs = new TaskCompletionStruct<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (outdatedTcs.IsEmpty)
                outdatedTcs = new TaskCompletionStruct<Unit>(TaskCreationOptions.RunContinuationsAsynchronously);
            Publication = publication;
            CreatedAt = IntMoment.Now;
            IsDisposed = isDisposed;
            InvalidatedTcs = invalidatedTcs;
            OutdatedTcs = outdatedTcs;
            Computed = computed;
            computed.Invalidated += (_, invalidatedBy) => InvalidatedTcs.TrySetResult(invalidatedBy);  
        }

        public Task<object?> InvalidatedAsync() => InvalidatedTcs.Task;
        public Task OutdatedAsync() => OutdatedTcs.Task;

        bool IPublicationStateImpl.TryMarkOutdated()
        {
            if (!OutdatedTcs.TrySetResult(default))
                return false;
            InvalidatedTcs.TrySetCanceled();
            return true;
        }
    }
}
