using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Bridge.Messages;
using Stl.Internal;
using Stl.Text;
using Stl.Time;

namespace Stl.Fusion.Bridge
{
    public interface IPublication : IHasId<Symbol>, IAsyncDisposable
    {
        IPublisher Publisher { get; }
        IPublicationState State { get; }
        
        Disposable<IPublication> Use();
        bool Touch();
        ValueTask UpdateAsync(CancellationToken cancellationToken);
    }

    public interface IPublication<T> : IPublication
    {
        new IPublicationState<T> State { get; }
    }

    public interface IPublicationImpl : IPublication, IAsyncProcess
    {
        SubscriptionProcessor CreateSubscriptionProcessor(Channel<Message> channel, SubscribeMessage subscribeMessage);
    }

    public interface IPublicationImpl<T> : IPublicationImpl, IPublication<T> { }

    public class Publication<T> : AsyncProcessBase, IPublicationImpl<T>
    {
        // Fields
        protected volatile IPublicationStateImpl<T> StateField;
        protected volatile int UserCount;
        protected volatile int LastTouchTime;
        // protected object Lock => new object();

        // Properties
        public Type PublicationType { get; }
        public IPublisher Publisher { get; }
        public Symbol Id { get; }
        IPublicationState IPublication.State => State;
        public IPublicationState<T> State => StateField;

        public Publication(Type publicationType, IPublisher publisher, IComputed<T> computed, Symbol id)
        {
            PublicationType = publicationType;
            Publisher = publisher;
            Id = id;
            LastTouchTime = IntMoment.Now.EpochOffsetUnits;
            StateField = CreatePublicationState(computed);
        }

        public bool Touch()
        {
            if (State.IsDisposed)
                return false;
            Interlocked.Exchange(ref LastTouchTime, IntMoment.Clock.EpochOffsetUnits);
            return true;
        }

        public Disposable<IPublication> Use()
        {
            if (State.IsDisposed)
                throw Errors.AlreadyDisposedOrDisposing();
            Interlocked.Increment(ref UserCount);
            return new Disposable<IPublication>(this, p => {
                var self = (Publication<T>) p;
                if (0 == Interlocked.Decrement(ref self.UserCount))
                    Touch();
            });
        }

        public async ValueTask UpdateAsync(CancellationToken cancellationToken)
        {
            var state = StateField;
            if (state.IsDisposed || state.Computed.IsConsistent)
                return;
            var newComputed = await state.Computed.UpdateAsync(cancellationToken).ConfigureAwait(false);
            var newState = CreatePublicationState(newComputed);
            ChangeState(newState, state);
        }

        protected virtual IPublicationStateImpl<T> CreatePublicationState(
            IComputed<T> computed, bool isDisposed = false) 
            => new PublicationState<T>(this, computed, isDisposed);

        protected override async Task RunInternalAsync(CancellationToken cancellationToken) 
        {
            try {
                await ExpireAsync(cancellationToken).ConfigureAwait(false);
            } 
            finally {
                // Awaiting for disposal here = cyclic task dependency;
                // we should just ensure it starts right when this method
                // completes.
                var _ = DisposeAsync();
            }
        }

        protected virtual async Task ExpireAsync(CancellationToken cancellationToken)
        {
            IntMoment GetLastUseTime() 
                => UserCount > 0 ? IntMoment.Now : new IntMoment(LastTouchTime);

            IntMoment GetNextExpirationCheckTime(IntMoment start, IntMoment lastUseTime)
            {
                // return lastUseTime + TimeSpan.FromMinutes(5); // Good for debugging 
                var now = IntMoment.Now;
                var lifetime = now.EpochOffsetUnits - start.EpochOffsetUnits;
                if (lifetime < IntMoment.UnitsPerSecond * 60) // 1 minute
                    return lastUseTime + TimeSpan.FromSeconds(10); 
                return lastUseTime + TimeSpan.FromSeconds(30); 
            }

            // return;
            try {
                var start = IntMoment.Now;
                var lastUseTime = GetLastUseTime();
                while (true) {
                    var nextCheckTime = GetNextExpirationCheckTime(start, lastUseTime);
                    var delay = TimeSpan.FromTicks(IntMoment.TicksPerUnit * Math.Max(0, nextCheckTime - IntMoment.Now));
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    var newLastUseTime = GetLastUseTime();
                    if (newLastUseTime == lastUseTime)
                        break;
                    lastUseTime = newLastUseTime;
                }
            }
            catch (OperationCanceledException) {
                // RunExpirationAsync is called w/o Task.Run, so there is a chance
                // it throws this exception synchronously (if cts got cancelled already).
            }
        }

        SubscriptionProcessor IPublicationImpl.CreateSubscriptionProcessor(Channel<Message> channel, SubscribeMessage subscribeMessage) 
            => CreateSubscriptionProcessor(channel, subscribeMessage);
        protected virtual SubscriptionProcessor CreateSubscriptionProcessor(Channel<Message> channel, SubscribeMessage subscribeMessage)
            => new SubscriptionProcessor<T>(this, channel, subscribeMessage);

        protected override Task DisposeAsync(bool disposing)
        {
            // We override this method to make sure State is the first thing
            // to reflect the disposal. 
            var state = StateField;
            if (state.IsDisposed)
                return Task.CompletedTask;
            var newState = CreatePublicationState(state.Computed, true);
            if (!ChangeState(newState, state))
                return Task.CompletedTask;
            return base.DisposeAsync(disposing);
        }

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            await base.DisposeInternalAsync(disposing).ConfigureAwait(false);
            if (Publisher is IPublisherImpl pi)
                pi.OnPublicationDisposed(this);
        }

        // State change & other low-level stuff

        protected bool ChangeState(IPublicationStateImpl<T> newState, IPublicationStateImpl<T> expectedState)
        {
            var oldState = Interlocked.CompareExchange(ref StateField, newState, expectedState);
            if (oldState != expectedState)
                return false;
            expectedState.TryMarkOutdated();
            return true;
        }
    }
}
