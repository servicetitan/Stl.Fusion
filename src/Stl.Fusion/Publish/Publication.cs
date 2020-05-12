using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Publish.Events;
using Stl.Fusion.Publish.Messages;
using Stl.Text;
using Stl.Time;

namespace Stl.Fusion.Publish
{
    public enum PublicationState
    {
        Updated = 0,
        Invalidated,
        Disposed,
    }

    public interface IPublication : IAsyncDisposable
    {
        Type PublicationType { get; }
        IPublisher Publisher { get; }
        Symbol Id { get; }
        IComputed Computed { get; }
        object? LastInvalidatedBy { get; }
        PublicationState State { get; }
        IntMoment LastUseTime { get; }
        IAsyncEventSource<PublicationStateChangedEvent> StateChangedEvents { get; }
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
        protected readonly AsyncEventSource<PublicationStateChangedEvent> StateChangedEventSource;
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
        public IntMoment LastUseTime => StateChangedEvents.HasObservers ? IntMoment.Now : new IntMoment(LastTouchTime);
        public IAsyncEventSource<PublicationStateChangedEvent> StateChangedEvents => StateChangedEventSource;


        protected IPublisherImpl PublisherImpl => (IPublisherImpl) Publisher; // Just a shortcut

        protected PublicationBase(Type publicationType, IPublisher publisher, IComputed<T> computed, Symbol id)
        {
            InvalidatedHandler = (_, invalidatedBy) => InvalidatedTcs?.SetResult(invalidatedBy);         
            StateChangedEventSource = new AsyncEventSource<PublicationStateChangedEvent>(ObserverCountChanged);
            PublicationType = publicationType;
            Publisher = publisher;
            Id = id;
            ChangeStateUnsafe(computed, PublicationState.Updated);
        }

        public bool Touch()
        {
            lock (Lock) {
                if (IsExpired || State == PublicationState.Disposed)
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
                var nextUpdateTime = IntMoment.MaxValue;
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    switch (nextState) {
                    case PublicationState.Updated:
                        var completedTask = await Task.WhenAny(InvalidatedTcs.Task, cancellationTask).ConfigureAwait(false);
                        if (completedTask == cancellationTask) {
                            nextState = PublicationState.Disposed;
                            break;
                        }
                        var invalidatedBy = await InvalidatedTcs.Task.ConfigureAwait(false);
                        Interlocked.Exchange(ref _lastInvalidatedBy, invalidatedBy);
                        nextState = PublicationState.Invalidated;
                        break;
                    case PublicationState.Invalidated:
                        if (nextUpdateTime == IntMoment.MaxValue) {
                            nextState = PublicationState.Disposed;
                            break;
                        }
                        var delay = TimeSpan.FromTicks((nextUpdateTime - IntMoment.Now) * IntMoment.TicksPerUnit);
                        if (delay > TimeSpan.Zero)
                            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        var maybeNextComputed = await Computed.UpdateAsync(cancellationToken).ConfigureAwait(false);
                        if (maybeNextComputed != null) {
                            nextComputed = maybeNextComputed;
                            nextState = PublicationState.Updated;
                        }
                        else
                            nextState = PublicationState.Disposed;
                        break;
                    case PublicationState.Disposed:
                        return; 
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                    var e = await ChangeStateAsync(nextComputed, nextState).ConfigureAwait(false);
                    if (e is PublicationInvalidatedEvent ie)
                        nextUpdateTime = ie.NextUpdateTime;
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
            if (State != PublicationState.Disposed)
                await ChangeStateAsync(Computed, PublicationState.Disposed).ConfigureAwait(false);
            await StateChangedEventSource.DisposeAsync(); // Same as CompleteAsync;
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
            PublisherImpl.OnPublicationDisposed(this);
        }

        // Expiration

        protected virtual void ObserverCountChanged(AsyncEventSource<PublicationStateChangedEvent, Unit> source, 
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
                await foreach (var e in StateChangedEvents.WithCancellation(cancellationToken).ConfigureAwait(false)) {
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

        protected virtual async ValueTask<PublicationStateChangedEvent> ChangeStateAsync(
            IComputed<T> nextComputed, PublicationState nextState)
        {
            PublicationStateChangedEvent e;
            lock (Lock) {
                ChangeStateUnsafe(nextComputed, nextState);
                e = CreateStateChangedEventUnsafe();
            }
            if (nextState != PublicationState.Disposed) 
                await StateChangedEventSource.NextAsync(e).ConfigureAwait(false); 
            else
                await StateChangedEventSource.LastAsync(e).ConfigureAwait(false);
            return e;
        }

        protected virtual PublicationStateChangedEvent CreateStateChangedEventUnsafe()
        {
            // Can be invoked from Lock-protected sections only
            PublicationStateChangedEvent e;
            switch (State) {
            case PublicationState.Updated:
                e = new PublicationUpdatedEvent(this, new UpdatedMessage<T>() {
                    Output = Computed.Output,
                    Tag = Computed.Tag,
                });
                break;
            case PublicationState.Invalidated:
                e = new PublicationInvalidatedEvent(this, new InvalidatedMessage() {
                    Tag = Computed.Tag,
                });
                break;
            case PublicationState.Disposed:
                e = new PublicationDisposedEvent(this, new DisposedMessage());
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            if (e.Message is PublicationMessage pm) {
                pm.PublisherId = Publisher.Id;
                pm.PublicationId = Id;
            }
            return e;
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

        protected override PublicationStateChangedEvent CreateStateChangedEventUnsafe()
        {
            var e = base.CreateStateChangedEventUnsafe();
            if (e is PublicationInvalidatedEvent ie)
                ie.VoteForNextUpdateTime(TimeSpan.FromSeconds(0.1));
            return e;
        }
    }

    public class NonUpdatingPublication<T> : PublicationBase<T>
    {
        public NonUpdatingPublication(Type publicationType, IPublisher publisher, IComputed<T> computed, Symbol id) 
            : base(publicationType, publisher, computed, id) 
        { }
    }
}
