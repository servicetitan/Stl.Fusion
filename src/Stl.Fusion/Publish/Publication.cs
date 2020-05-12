using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
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

    public interface IPublication : IAsyncEnumerable<PublicationStateChanged>, IAsyncDisposable
    {
        Type PublicationType { get; }
        IPublisher Publisher { get; }
        Symbol Id { get; }
        IComputed Computed { get; }
        object? LastInvalidatedBy { get; }
        PublicationState State { get; }
        bool HasObservers { get; }
        IntMoment LastUseTime { get; }
        bool Touch();
    }

    public interface IPublication<T> : IPublication
    {
        new IComputed<T> Computed { get; }
    }

    public interface IPublicationImpl : IPublication, IAsyncProcess
    {
        Task RunSubscriptionAsync(Channel<Message> channel, bool notify, CancellationToken cancellationToken);
    }

    public interface IPublicationImpl<T> : IPublicationImpl, IPublication<T> { }

    public abstract class PublicationBase<T> : AsyncProcessBase, IPublicationImpl<T>
    {
        private volatile IComputed<T> _computed = null!;
        private volatile PublicationState _state;
        private volatile object? _lastInvalidatedBy;

        protected readonly Action<IComputed, object?> InvalidatedHandler; 
        protected readonly AsyncEventSource<PublicationStateChanged> StateChangeEventSource;
        protected volatile TaskCompletionSource<object?> InvalidatedTcs = null!;
        protected volatile int LastTouchTime;
        protected bool IsExpired;
        protected object Lock => InvalidatedHandler;

        public Type PublicationType { get; }
        public IPublisher Publisher { get; }
        public Symbol Id { get; }
        IComputed IPublication.Computed => _computed;
        public IComputed<T> Computed => _computed;
        public object? LastInvalidatedBy => _lastInvalidatedBy;
        public PublicationState State => _state;
        public bool HasObservers => StateChangeEventSource.HasObservers;
        public IntMoment LastUseTime => HasObservers ? IntMoment.Now : new IntMoment(LastTouchTime);

        protected IPublisherImpl PublisherImpl => (IPublisherImpl) Publisher; // Just a shortcut

        protected PublicationBase(Type publicationType, IPublisher publisher, IComputed<T> computed, Symbol id)
        {
            InvalidatedHandler = (_, invalidatedBy) => InvalidatedTcs?.SetResult(invalidatedBy);         
            StateChangeEventSource = new AsyncEventSource<PublicationStateChanged>(ObserverCountChanged);
            PublicationType = publicationType;
            Publisher = publisher;
            Id = id;
            ChangeStateUnsafe(computed, PublicationState.Consistent);
        }

        public IAsyncEnumerator<PublicationStateChanged> GetAsyncEnumerator(CancellationToken cancellationToken = default) 
            => StateChangeEventSource.GetAsyncEnumerator(cancellationToken);

        public bool Touch()
        {
            lock (Lock) {
                if (IsExpired || State == PublicationState.Unpublished)
                    return false;
                LastTouchTime = IntMoment.Clock.EpochOffsetUnits;
                return true;
            }
        }

        protected override async Task RunInternalAsync(CancellationToken cancellationToken) 
        {
            // We intend to just start this task; no need to wait
            var _ = ExpireAsync(cancellationToken);

            var cancellationTask = cancellationToken.ToTask(false);
            try {
                var nextState = State;
                var nextComputed = Computed;
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    bool mustContinue;
                    switch (nextState) {
                    case PublicationState.Consistent:
                        var completedTask = await Task.WhenAny(InvalidatedTcs.Task, cancellationTask).ConfigureAwait(false);
                        if (completedTask == cancellationTask) {
                            nextState = PublicationState.Unpublished;
                            break;
                        }
                        var invalidatedBy = await InvalidatedTcs.Task.ConfigureAwait(false);
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
                    await ChangeStateAsync(nextComputed, nextState).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) {
                // It's an absolutely "legal" exit
            } 
            finally {
                await DisposeAsync().ConfigureAwait(false);
            }
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            lock (Lock) {
                IsExpired = true;
            }
            if (State != PublicationState.Unpublished)
                await ChangeStateAsync(Computed, PublicationState.Unpublished).ConfigureAwait(false);
            await StateChangeEventSource.DisposeAsync(); // Same as CompleteAsync;
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
            PublisherImpl.OnPublicationDisposed(this);
        }

        // Lifetime events

        protected abstract bool OnInvalidated(); 

        protected virtual Task<bool> OnUpdatePendingAsync(CancellationToken cancellationToken)
            => TaskEx.TrueTask;

        protected virtual ValueTask<IComputed<T>?> OnUpdatingAsync(CancellationToken cancellationToken) 
            => Computed.UpdateAsync(cancellationToken);

        // Expiration

        protected virtual void ObserverCountChanged(AsyncEventSource<PublicationStateChanged, Unit> source, 
            int observerCount, bool isAdded)
        {
            if (observerCount == 0)
                Touch();
        }

        protected virtual async Task ExpireAsync(CancellationToken cancellationToken)
        {
            try {
                var start = IntMoment.Now;
                var lastUseTime = LastUseTime;
                while (true) {
                    var nextCheckTime = GetNextExpirationCheckTime(start, lastUseTime);
                    var delay = TimeSpan.FromTicks(IntMoment.TicksPerUnit * Math.Min(0, nextCheckTime - IntMoment.Now));
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    var newLastUseTime = LastUseTime;
                    if (newLastUseTime == lastUseTime) {
                        lock (Lock) {
                            // We should re-check everything inside locked section
                            // to make sure Touch returns true IFF this method 
                            // isn't going to expire the item.
                            newLastUseTime = LastUseTime;
                            if (newLastUseTime == lastUseTime) {
                                IsExpired = true;
                                break;
                            }
                        }
                    }
                    lastUseTime = newLastUseTime;
                }
            }
            catch (OperationCanceledException) {
                // RunExpirationAsync is called w/o Task.Run, so there is a chance
                // it throws this exception synchronously (if cts got cancelled already).
            }
        }

        private IntMoment GetNextExpirationCheckTime(IntMoment start, IntMoment lastUseTime)
        {
            var now = IntMoment.Now;
            var lifetime = now.EpochOffsetUnits - start.EpochOffsetUnits;
            if (lifetime < IntMoment.UnitsPerSecond * 60) // 1 minute
                return lastUseTime + TimeSpan.FromSeconds(10); 
            return lastUseTime + TimeSpan.FromSeconds(30); 
        }

        // Subscription

        Task IPublicationImpl.RunSubscriptionAsync(Channel<Message> channel, bool notify, CancellationToken cancellationToken) 
            => RunSubscriptionAsync(channel, notify, cancellationToken);
        protected virtual async Task RunSubscriptionAsync(Channel<Message> channel, bool notify, CancellationToken cancellationToken)
        {
            var writer = channel.Writer;
            try {
                if (notify)
                    await NotifySubscribeAsync(channel, cancellationToken).ConfigureAwait(false);
                await foreach (var e in this.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                    var message = e.Message;
                    if (message != null)
                        await writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                if (notify && await writer.WaitToWriteAsync(cancellationToken))
                    await NotifyUnsubscribeAsync(channel, cancellationToken).ConfigureAwait(false);
            }
        }

        protected virtual ValueTask NotifySubscribeAsync(Channel<Message> channel, CancellationToken cancellationToken)
        {
            var message = new SubscribeMessage() {
                PublisherId = Publisher.Id,
                PublicationId = Id,
            };
            return channel.Writer.WriteAsync(message, cancellationToken);
        }

        protected virtual ValueTask NotifyUnsubscribeAsync(Channel<Message> channel, CancellationToken cancellationToken)
        {
            var message = new UnsubscribeMessage() {
                PublisherId = Publisher.Id,
                PublicationId = Id,
            };
            return channel.Writer.WriteAsync(message, cancellationToken);
        }

        // State change & other low-level stuff

        protected virtual ValueTask ChangeStateAsync(IComputed<T> nextComputed, PublicationState nextState)
        {
            PublicationStateChanged eventInfo;
            lock (Lock) {
                ChangeStateUnsafe(nextComputed, nextState);
                var message = CreateStateChangedMessageUnsafe();
                eventInfo = new PublicationStateChanged(this, message);
            }
            return nextState != PublicationState.Unpublished 
                ? StateChangeEventSource.NextAsync(eventInfo) 
                : StateChangeEventSource.LastAsync(eventInfo);
        }

        protected virtual Message? CreateStateChangedMessageUnsafe()
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

        protected void ChangeStateUnsafe(IComputed<T> nextComputed, PublicationState nextState)
        {
            // Can be invoked from Lock-protected sections only
            _state = nextState;
            if (_computed == nextComputed)
                return;
            InvalidatedTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _computed = nextComputed;
            nextComputed.Invalidated += InvalidatedHandler;
        }
    }

    public class UpdatingPublication<T> : PublicationBase<T>
    {
        public UpdatingPublication(Type publicationType, IPublisher publisher, IComputed<T> computed, Symbol id) 
            : base(publicationType, publisher, computed, id)              
        { }

        protected override bool OnInvalidated() => true;
    }

    public class NonUpdatingPublication<T> : PublicationBase<T>
    {
        public NonUpdatingPublication(Type publicationType, IPublisher publisher, IComputed<T> computed, Symbol id) 
            : base(publicationType, publisher, computed, id) 
        { }

        protected override bool OnInvalidated() => false;
    }
}
