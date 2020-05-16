using System;
using System.Reactive;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Bridge.Events;
using Stl.Fusion.Bridge.Messages;
using Stl.Text;
using Stl.Time;

namespace Stl.Fusion.Bridge
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
        Task RunSubscriptionAsync(Channel<PublicationMessage> channel, bool sendUpdate, CancellationToken cancellationToken);
    }

    public interface IPublicationImpl<T> : IPublicationImpl, IPublication<T> { }

    public class Publication<T> : AsyncProcessBase, IPublicationImpl<T>
    {
        // Fields
        private volatile IComputed<T> _computed = null!;
        private volatile PublicationState _state;
        private volatile object? _lastInvalidatedBy;
        protected readonly Action<IComputed, object?> InvalidatedHandler; 
        protected readonly AsyncEventSource<PublicationStateChangedEvent> StateChangedEventSource;
        protected volatile TaskCompletionSource<object?> InvalidatedTcs = null!;
        protected volatile int LastTouchTime;
        protected bool IsExpired;
        protected object Lock => InvalidatedHandler;

        // Properties
        public Type PublicationType { get; }
        public IPublisher Publisher { get; }
        public Symbol Id { get; }
        IComputed IPublication.Computed => _computed;
        public IComputed<T> Computed => _computed;
        public object? LastInvalidatedBy => _lastInvalidatedBy;
        public PublicationState State => _state;
        public IntMoment LastUseTime => StateChangedEvents.HasObservers ? IntMoment.Now : new IntMoment(LastTouchTime);
        public IAsyncEventSource<PublicationStateChangedEvent> StateChangedEvents => StateChangedEventSource;

        public Publication(Type publicationType, IPublisher publisher, IComputed<T> computed, Symbol id)
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
            if (Publisher is IPublisherImpl pi)
                pi.OnPublicationDisposed(this);
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
            // return;
            try {
                var start = IntMoment.Now;
                var lastUseTime = LastUseTime;
                while (true) {
                    var nextCheckTime = GetNextExpirationCheckTime(start, lastUseTime);
                    var delay = TimeSpan.FromTicks(IntMoment.TicksPerUnit * Math.Min(0, nextCheckTime - IntMoment.Now));
                    if (delay > TimeSpan.Zero)
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

        Task IPublicationImpl.RunSubscriptionAsync(
            Channel<PublicationMessage> channel, bool sendUpdate, CancellationToken cancellationToken) 
            => RunSubscriptionAsync(channel, sendUpdate, cancellationToken);
        protected virtual async Task RunSubscriptionAsync(
            Channel<PublicationMessage> channel, bool sendUpdate, CancellationToken cancellationToken)
        {
            var writer = channel.Writer;
            try {
                if (sendUpdate) {
                    Touch(); // In case the next call takes a while 
                    await SendUpdateAsync(channel, cancellationToken).ConfigureAwait(false);
                }
                await foreach (var e in StateChangedEvents.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                    var message = e.Message;
                    if (message != null)
                        await writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                var cts = (CancellationTokenSource?) null;
                try {
                    if (cancellationToken.IsCancellationRequested) {
                        cts = new CancellationTokenSource();
                        cts.CancelAfter(10_000);
                        cancellationToken = cts.Token;
                    }
                }
                catch (OperationCanceledException) {
                    // Ignore: it's totally fine to see it here
                }
                finally {
                    cts?.Dispose();
                }
            }
        }

        protected virtual async ValueTask SendUpdateAsync(
            Channel<PublicationMessage> channel, CancellationToken cancellationToken)
        {
            var computed = Computed;
            if (!computed.IsConsistent)
                computed = await computed.UpdateAsync(cancellationToken).ConfigureAwait(false);
            
            var message = new UpdatedMessage<T>() {
                PublisherId = Publisher.Id,
                PublicationId = Id,
                Output = computed.Output,
                Tag = computed.Tag,
                FromTag = 0,
            };
            await channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        }

        // State change & other low-level stuff

        protected virtual async ValueTask<PublicationStateChangedEvent> ChangeStateAsync(
            IComputed<T> nextComputed, PublicationState nextState)
        {
            PublicationStateChangedEvent e;
            lock (Lock) {
                var prevComputed = _computed;
                ChangeStateUnsafe(nextComputed, nextState);
                e = CreateStateChangedEventUnsafe(prevComputed);
            }
            if (nextState != PublicationState.Disposed) 
                await StateChangedEventSource.NextAsync(e).ConfigureAwait(false); 
            else
                await StateChangedEventSource.LastAsync(e).ConfigureAwait(false);
            return e;
        }

        protected virtual PublicationStateChangedEvent CreateStateChangedEventUnsafe(IComputed<T> prevComputed)
        {
            // Can be invoked from Lock-protected sections only
            PublicationStateChangedEvent e;
            switch (State) {
            case PublicationState.Updated:
                e = new PublicationUpdatedEvent(this, new UpdatedMessage<T>() {
                    Output = Computed.Output,
                    Tag = Computed.Tag,
                    FromTag = prevComputed?.Tag ?? 0,
                });
                break;
            case PublicationState.Invalidated:
                e = new PublicationInvalidatedEvent(this, new InvalidatedMessage() {
                    Tag = Computed.Tag,
                }, IntMoment.Now + TimeSpan.FromSeconds(0.1));
                break;
            case PublicationState.Disposed:
                e = new PublicationDisposedEvent(this, new DisposedMessage());
                break;
            default:
                throw new ArgumentOutOfRangeException();
            }

            var message = e.Message;
            if (message != null) {
                message.PublisherId = Publisher.Id;
                message.PublicationId = Id;
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

    public class NonUpdatingPublication<T> : Publication<T>
    {
        public NonUpdatingPublication(Type publicationType, IPublisher publisher, IComputed<T> computed, Symbol id) 
            : base(publicationType, publisher, computed, id)              
        { }

        protected override PublicationStateChangedEvent CreateStateChangedEventUnsafe(IComputed<T> prevComputed)
        {
            var e = base.CreateStateChangedEventUnsafe(prevComputed);
            if (e is PublicationInvalidatedEvent ie)
                ie.NextUpdateTime = IntMoment.MaxValue;
            return e;
        }
    }
}
