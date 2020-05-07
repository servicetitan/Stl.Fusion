using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;
using Stl.Collections.Slim;
using Stl.Fusion.Publish.Messages;
using Stl.Text;
using Stl.Time;

namespace Stl.Fusion.Publish
{
    public enum PublicationState
    {
        Consistent = 0,
        Invalidated,
        UpdatePending,
        Updating,
        Unpublished,
    }

    public interface IPublication
    {
        IPublisher Publisher { get; }
        Symbol Id { get; }
        IComputed Computed { get; }
        object? LastInvalidatedBy { get; }
        PublicationState State { get; }
        bool HasHandlers { get; }
        IntMoment LastHandlersChangedTime { get; }
        
        bool AddHandler(IPublicationHandler handler);
        bool RemoveHandler(IPublicationHandler handler);
    }

    public interface IPublication<T> : IPublication
    {
        new IComputed<T> Computed { get; }
    }

    public interface IPublicationImpl : IPublication
    {
        Task PublishAsync(CancellationToken cancellationToken);
        Task DelayedUnpublishAsync(CancellationToken cancellationToken);
    }

    public interface IPublicationImpl<T> : IPublicationImpl, IPublication<T> { }

    public abstract class PublicationBase<T> : IPublicationImpl<T>
    {
        private RefHashSetSlim4<IPublicationHandler> _handlers = default;
        private readonly Action<IComputed, object?> _cachedInvalidatedHandler; 
        private volatile IComputed<T> _computed = null!;
        private volatile object? _lastInvalidatedBy;
        private volatile PublicationState _state;
        private volatile TaskCompletionSource<object?> _invalidatedTcs = null!;
        private volatile bool _hasHandlers;
        private volatile int _lastHandlersChangedTime;

        protected object Lock => _cachedInvalidatedHandler;
        protected RefHashSetSlim4<IPublicationHandler> Handlers => _handlers; 
        protected TaskCompletionSource<object?> InvalidatedTcs => _invalidatedTcs;
        protected IPublisherImpl PublishedImpl => (IPublisherImpl) Publisher;

        public IPublisher Publisher { get; }
        public Symbol Id { get; }
        IComputed IPublication.Computed => _computed;
        public IComputed<T> Computed => _computed;
        public object? LastInvalidatedBy => _lastInvalidatedBy;
        public PublicationState State => _state;
        public bool HasHandlers => _hasHandlers;
        public IntMoment LastHandlersChangedTime => new IntMoment(_lastHandlersChangedTime);

        protected PublicationBase(IPublisher publisher, IComputed<T> computed, Symbol id)
        {
            _cachedInvalidatedHandler = OnInvalidated;
            _lastHandlersChangedTime = IntMoment.Clock.EpochOffsetUnits;
            Publisher = publisher;
            Id = id;
            UpdateUnsafe(computed, PublicationState.Consistent);
        }

        public bool AddHandler(IPublicationHandler handler)
        {
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            lock (Lock) {
                if (State == PublicationState.Unpublished || !_handlers.Add(handler))
                    return false;
                _lastHandlersChangedTime = IntMoment.Clock.EpochOffsetUnits;
                OnHandlerAdded(handler);
                var handlerCount = _handlers.Count;
                _hasHandlers = handlerCount != 0;
                if (handlerCount == 1)
                    PublishedImpl.EndDelayedUnpublish(this);
            }
            return true;
        }

        public bool RemoveHandler(IPublicationHandler handler)
        {
            lock (Lock) {
                if (!_handlers.Remove(handler))
                    return false;
                _lastHandlersChangedTime = IntMoment.Clock.EpochOffsetUnits;
                OnHandlerRemoved(handler);
                var handlerCount = _handlers.Count;
                _hasHandlers = handlerCount != 0;
                if (handlerCount == 0)
                    PublishedImpl.BeginDelayedUnpublish(this);
            }
            return true;
        }

        Task IPublicationImpl.PublishAsync(CancellationToken cancellationToken) 
            => PublishAsync(cancellationToken);
        protected virtual async Task PublishAsync(CancellationToken cancellationToken)
        {
            var cancellationTask = cancellationToken.ToTask(false);
            try {
                var nextState = State;
                var nextComputed = Computed;
                while (true) {
                    bool mustContinue;
                    switch (nextState) {
                    case PublicationState.Consistent:
                        var completedTask = await Task.WhenAny(_invalidatedTcs.Task, cancellationTask).ConfigureAwait(false);
                        if (completedTask == cancellationTask) {
                            nextState = PublicationState.Unpublished;
                            break;
                        }
                        var invalidatedBy = await _invalidatedTcs.Task.ConfigureAwait(false);
                        Interlocked.Exchange(ref _lastInvalidatedBy, invalidatedBy);
                        nextState = PublicationState.Invalidated;
                        break;
                    case PublicationState.Invalidated:
                        mustContinue = OnInvalidated();
                        nextState = mustContinue
                            ? PublicationState.UpdatePending
                            : PublicationState.Unpublished;
                        break;
                    case PublicationState.UpdatePending:
                        mustContinue = await OnUpdatePendingAsync(cancellationToken).ConfigureAwait(false);
                        nextState = mustContinue
                            ? PublicationState.Updating
                            : PublicationState.Unpublished;
                        break;
                    case PublicationState.Updating:
                        var maybeNextComputed = await OnUpdatingAsync(cancellationToken).ConfigureAwait(false);
                        if (maybeNextComputed != null) {
                            nextComputed = maybeNextComputed;
                            nextState = PublicationState.Consistent;
                        }
                        else
                            nextState = PublicationState.Unpublished;
                        break;
                    case PublicationState.Unpublished:
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    await UpdateAsync(nextComputed, nextState, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) {
                if (!cancellationToken.IsCancellationRequested)
                    throw; 
                if (State != PublicationState.Unpublished)
                    await UpdateAsync(Computed, PublicationState.Unpublished, default).ConfigureAwait(false);
            }
            // Nothing else is here b/c this method is supposed to be called from
            // a wrapping async method doing the rest (i.e. the actual "unpublish").
        }

        Task IPublicationImpl.DelayedUnpublishAsync(CancellationToken cancellationToken) 
            => DelayedUnpublishAsync(cancellationToken);
        protected virtual async Task DelayedUnpublishAsync(CancellationToken cancellationToken)
        {
            try {
                await Task.Delay(GetUnpublishDelay(), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                // Not quite needed, but since cancellation is used here
                // as a "legal" termination signal, let's suppress the exception
                // + make sure callers don't have to worry about this.
                return; 
            }
            await PublishedImpl.UnpublishAsync(this).ConfigureAwait(false);
        }

        protected virtual TimeSpan GetUnpublishDelay() => TimeSpan.FromMinutes(1.5);

        // Protected virtual

        protected virtual Task UpdateAsync(IComputed<T> nextComputed, PublicationState nextState, CancellationToken cancellationToken)
        {
            var bTasks = ListBuffer<Task>.Lease(); 
            ListBuffer<IPublicationHandler> bHandlers = default;
            try {
                PublicationState previousState;
                Message? message;
                lock (Lock) {
                    previousState = _state;
                    UpdateUnsafe(nextComputed, nextState);
                    var handlerCount = _handlers.Count;
                    if (handlerCount == 0)
                        return Task.CompletedTask; 
                    bHandlers = ListBuffer<IPublicationHandler>.LeaseAndSetCount(handlerCount);
                    _handlers.CopyTo(bHandlers.Span);
                    message = CreateMessageUnsafe();
                }
                foreach (var handler in bHandlers.Span) {
                    var task = handler.OnStateChangedAsync(this, previousState, message, cancellationToken);
                    if (!task.IsCompletedSuccessfully)
                        bTasks.Add(task);
                }
                return bTasks.Count switch {
                    0 => Task.CompletedTask,
                    1 => bTasks[0],
                    _ => Task.WhenAll(bTasks.ToArray())
                };
            }
            finally {
                bTasks.Release();
                bHandlers.Release();
            }
        }

        protected abstract bool OnInvalidated(); 

        protected virtual Task<bool> OnUpdatePendingAsync(CancellationToken cancellationToken)
            => TaskEx.TrueTask;

        protected virtual ValueTask<IComputed<T>?> OnUpdatingAsync(CancellationToken cancellationToken) 
            => Computed.UpdateAsync(cancellationToken);

        protected virtual void OnHandlerAdded(IPublicationHandler handler)
        {
            // Let's send the "last" message to the new handler immediately
            var message = CreateMessageUnsafe();
            Task.Run(() => handler.OnStateChangedAsync(this, State, message, default));
        }

        protected virtual void OnHandlerRemoved(IPublicationHandler handler) { }

        protected virtual Message? CreateMessageUnsafe()
        {
            // Can be invoked from Lock-protected sections only
            var message = (Message?) null;
            switch (State) {
            case PublicationState.Consistent:
                message = new ConsistentMessage<T>() {
                    Output = Computed.Output,
                    Tag = Computed.Tag,
                };
                break;
            case PublicationState.Invalidated:
                message = new InvalidatedMessage() {
                    Tag = Computed.Tag,
                };
                break;
            case PublicationState.UpdatePending:
            case PublicationState.Updating:
                break;
            case PublicationState.Unpublished:
                message = new UnpublishedMessage();
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            if (message is PublicationMessage pm) {
                pm.PublisherId = Publisher.Id;
                pm.PublicationId = Id;
            }
            return message;
        }

        // Protected

        protected void UpdateUnsafe(IComputed<T> nextComputed, PublicationState nextState)
        {
            // Can be invoked from Lock-protected sections only
            _state = nextState;
            if (_computed == nextComputed)
                return;
            _invalidatedTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _computed = nextComputed;
            nextComputed.Invalidated += _cachedInvalidatedHandler;
        }
        
        // Private

        private void OnInvalidated(IComputed computed, object? invalidatedBy) 
            => _invalidatedTcs?.SetResult(invalidatedBy);
    }

    public class UpdatingPublication<T> : PublicationBase<T>
    {
        public UpdatingPublication(IPublisher publisher, IComputed<T> computed, Symbol id) 
            : base(publisher, computed, id) 
        { }

        protected override bool OnInvalidated() => true;
    }

    public class NonUpdatingPublication<T> : PublicationBase<T>
    {
        public NonUpdatingPublication(IPublisher publisher, IComputed<T> computed, Symbol id) 
            : base(publisher, computed, id) 
        { }

        protected override bool OnInvalidated() => false;
    }
}
