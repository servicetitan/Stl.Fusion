using Stl.Fusion.Bridge.Internal;
using Errors = Stl.Internal.Errors;

namespace Stl.Fusion.Bridge;

public interface IPublication : IDisposable
{
    IPublisher Publisher { get; }
    Symbol Id { get; }
    PublicationRef Ref { get; }
    PublicationState State { get; }
    long UseCount { get; }
    Moment LastUseTime { get; }

    Type GetResultType();
    Disposable<IPublication> Use();
    bool TryTouch();
    void Touch();
    ValueTask Update(CancellationToken cancellationToken);
    Task Expire();

    // Convenience helpers
    TResult Apply<TArg, TResult>(IPublicationApplyHandler<TArg, TResult> handler, TArg arg);
}

public interface IPublication<T> : IPublication
{
    new PublicationState<T> State { get; }
}

public class Publication<T> : IPublication<T>
{
    private readonly IMomentClock _clock;
    private volatile PublicationState<T> _state;
    private long _lastTouchTime;
    private long _useCount;
    private CancellationTokenSource _disposeTokenSource;
    private CancellationToken _disposeToken;

    private Moment LastTouchTime {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Volatile.Read(ref _lastTouchTime));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Volatile.Write(ref _lastTouchTime, value.EpochOffset.Ticks);
    }

    // Properties
    public Type PublicationType { get; }
    public IPublisher Publisher { get; }
    public Symbol Id { get; }
    public PublicationRef Ref => new(Publisher.Id, Id);
    PublicationState IPublication.State => _state;
    public PublicationState<T> State => _state;
    public long UseCount => Interlocked.Read(ref _useCount);
    public Moment LastUseTime => UseCount > 0 ? _clock.Now : LastTouchTime;

    public Publication(
        Type publicationType, IPublisher publisher,
        IComputed<T> computed, Symbol id,
        IMomentClock? clock)
    {
        PublicationType = publicationType;
        Publisher = publisher;
        Id = id;
        _clock = clock ??= MomentClockSet.Default.CoarseCpuClock;
        // ReSharper disable once VirtualMemberCallInConstructor
        _state = new PublicationState<T>(this, computed, false);
        _disposeTokenSource = new CancellationTokenSource();
        _disposeToken = _disposeTokenSource.Token;
        LastTouchTime = clock.Now;
    }

    public void Dispose()
    {
        // We override this method to make sure State is the first thing
        // to reflect the disposal.
        var state = State;
        if (state.IsDisposed)
            return;
        var newState = new PublicationState<T>(this, state.Computed, true);
        var spinWait = new SpinWait();
        while (true) {
            var currentState = ChangeState(newState, state);
            if (currentState == state)
                break;
            state = currentState;
            if (state.IsDisposed)
                return;
            spinWait.SpinOnce();
        }
        _disposeTokenSource.Cancel();
        _disposeTokenSource.Dispose();
        if (Publisher is IPublisherImpl pi)
            pi.OnPublicationDisposed(this);
    }

    public Type GetResultType()
        => typeof(T);

    public bool TryTouch()
    {
        LastTouchTime = _clock.Now;
        return !State.IsDisposed;
    }

    public void Touch()
    {
        if (!TryTouch())
            throw Errors.AlreadyDisposed();
    }

    public Disposable<IPublication> Use()
    {
        Interlocked.Increment(ref _useCount);
        if (State.IsDisposed) {
            Interlocked.Decrement(ref _useCount);
            throw Errors.AlreadyDisposedOrDisposing();
        }
        return new Disposable<IPublication>(this, p => {
            var self = (Publication<T>) p;
            TryTouch();
            Interlocked.Decrement(ref self._useCount);
        });
    }

    public async ValueTask Update(CancellationToken cancellationToken)
    {
        while (true) {
            var state = _state;
            if (state.IsDisposed || state.Computed.IsConsistent())
                return;
            var newComputed = await state.Computed.Update(cancellationToken).ConfigureAwait(false);
            var newState = new PublicationState<T>(this, newComputed, false);
            if (ChangeState(newState, state) == state)
                return;
        }
    }

    public async Task Expire()
    {
        // Uncomment for debugging:
        // await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
        try {
            while (true) {
                var delay = GetNextCheckTime(LastUseTime) - _clock.Now;
                if (delay <= TimeSpan.Zero)
                    break; // Expired
                await _clock.Delay(delay, _disposeToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) {
            // Intended
        }
    }

    public TResult Apply<TArg, TResult>(IPublicationApplyHandler<TArg, TResult> handler, TArg arg)
        => handler.Apply(this, arg);

    // State change & other low-level stuff

    private PublicationState<T> ChangeState(PublicationState<T> newState, PublicationState<T> expectedState)
    {
        var oldState = Interlocked.CompareExchange(ref _state, newState, expectedState);
        if (oldState != expectedState)
            return oldState;
        return oldState;
    }

    private Moment GetNextCheckTime(Moment lastUseTime)
        => lastUseTime + Publisher.Options.PublicationExpirationTime;
}
