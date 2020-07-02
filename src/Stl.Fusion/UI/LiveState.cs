using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Internal;
using Stl.Reflection;
using Errors = Stl.Fusion.Internal.Errors;

namespace Stl.Fusion.UI
{
    public interface ILiveState : IResult, IDisposable
    {
        IComputed State { get; }
        Exception? LastUpdateError { get; }
        IUpdateDelayer UpdateDelayer { get; }
        event Action<ILiveState>? Updated;

        void Invalidate(bool updateImmediately = true);
    }

    public interface ILiveState<TState> : ILiveState, IResult<TState>
    {
        new IComputed<TState> State { get; }
    }

    public interface ILiveState<TLocal, TState> : ILiveState<TState>
    {
        TLocal Local { get; set; }
    }

    public abstract class LiveState
    {
        public abstract class Options
        {
            public bool IsolateUpdateErrors { get; set; } = true;
            public bool DelayFirstUpdate { get; set; } = false;
            public IUpdateDelayer? UpdateDelayer { get; set; } = null;
        }
    }

    public class LiveState<TLocal, TState> : LiveState, ILiveState<TLocal, TState>
    {
        public new class Options : LiveState.Options
        {
            public TLocal InitialLocal { get; set; } = ActivatorEx.New<TLocal>(false);
            public Result<TState> InitialState { get; set; } = ActivatorEx.New<TState>(false)!;
            public Func<ILiveState<TLocal, TState>, CancellationToken, Task<TState>> Updater { get; set; } = null!;
        }

        private readonly ILogger _log; 
        private readonly Func<ILiveState<TLocal, TState>, CancellationToken, Task<TState>> _updater;
        private readonly bool _isolateUpdateErrors; 
        private readonly bool _delayFirstUpdate; 
        private readonly Action<IComputed> _invalidationHandler;
        private readonly CancellationTokenSource _disposeCts;
        private readonly CancellationToken _disposeCtsToken;
        private volatile IComputed<TState>? _trackedComputed;
        private volatile Box<TLocal> _local;
        private SimpleComputedInput<TState>? _computedRef;
        private volatile Exception? _lastUpdateError;
        private volatile int _failedUpdateIndex;
        private volatile int _updateIndex;

        public TLocal Local {
            get => _local.Value;
            set { _local = Box.New(value); Invalidate(); }
        }
        IComputed ILiveState.State => State;
        public IComputed<TState> State => _computedRef!.Computed;
        public Exception? LastUpdateError => _lastUpdateError;
        public IUpdateDelayer UpdateDelayer { get; }
        public event Action<ILiveState>? Updated;

        // IResult<T> & IResult
        object? IResult.UnsafeValue => UnsafeValue;
        public TState UnsafeValue => State.UnsafeValue;
        object? IResult.Value => Value;
        public TState Value => State.Value;
        public Exception? Error => State.Error;
        public bool HasValue => State.HasValue;
        public bool HasError => State.HasError;

        public LiveState(
            Options options,
            IUpdateDelayer? updateDelayer = null,
            ILogger<LiveState<TState>>? log = null)
        {
            _log = log ??= NullLogger<LiveState<TState>>.Instance;

            _updater = options.Updater 
                ?? throw new ArgumentNullException(nameof(options) + "." + nameof(options.Updater));
            _delayFirstUpdate = options.DelayFirstUpdate;
            _isolateUpdateErrors = options.IsolateUpdateErrors;
            UpdateDelayer = options.UpdateDelayer ?? updateDelayer ?? UI.UpdateDelayer.Default;

            _invalidationHandler = OnInvalidated;
            _disposeCts = new CancellationTokenSource();
            _disposeCtsToken = _disposeCts.Token;
            _local = Box.New(options.InitialLocal);
            Start(options.InitialState);
        }

        ~LiveState() => Dispose();

        public void Dispose()
        {
            if (_trackedComputed == null || _disposeCts.IsCancellationRequested)
                return;
            GC.SuppressFinalize(this);
            BeginTracking(null);
            try {
                _disposeCts.Cancel();
            }
            finally {
                _disposeCts.Dispose();
            }
        }

        public void Deconstruct(out TState value, out Exception? error)
            => State.Deconstruct(out value, out error);

        public void ThrowIfError() => State.ThrowIfError();
        public bool IsValue([MaybeNullWhen(false)] out TState value) 
            => State.IsValue(out value);
        public bool IsValue([MaybeNullWhen(false)] out TState value, [MaybeNullWhen(true)] out Exception error) 
            => State.IsValue(out value, out error!);

        public void Invalidate(bool updateImmediately = true)
        {
            State.Invalidate();
            if (updateImmediately)
                UpdateDelayer.CancelDelays();
        }

        private void Start(Result<TState> @default)
        {
            var computed = SimpleComputed.New(UpdateAsync, @default, false);
            var oldComputedRef = Interlocked.CompareExchange(
                ref _computedRef, computed.Input, null);
            if (oldComputedRef != null)
                throw Errors.AlreadyStarted();
            BeginTracking(computed);
        }

        private void BeginTracking(IComputed<TState>? newComputed)
        {
            var oldComputed = Interlocked.Exchange(ref _trackedComputed, newComputed);
            if (oldComputed != null)
                oldComputed.Invalidated -= _invalidationHandler;
            if (newComputed != null)
                newComputed.Invalidated += _invalidationHandler;
            if (oldComputed?.IsConsistent == true)
                _log.LogWarning($"{nameof(BeginTracking)}: oldComputed.IsConsistent == true.");
        }

        private void OnInvalidated(IComputed computed)
        {
            using var _ = ExecutionContext.SuppressFlow();
            Task.Run(async () => {
                var updateIndex = Interlocked.Increment(ref _updateIndex) - 1;
                var cancellationToken = _disposeCtsToken;
                try {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (updateIndex != 0 || _delayFirstUpdate)
                        await UpdateDelayer.DelayAsync(cancellationToken).ConfigureAwait(false);
                    await computed.UpdateAsync(cancellationToken).ConfigureAwait(false);
                }
                finally {
                    cancellationToken.ThrowIfCancellationRequested();
                    Updated?.Invoke(this);
                }
            }, CancellationToken.None);
        }

        private async Task UpdateAsync(
            IComputed<TState> prev, IComputed<TState> next, 
            CancellationToken cancellationToken)
        {
            try {
                var value = await _updater.Invoke(this, cancellationToken)
                    .ConfigureAwait(false);
                next.SetOutput(new Result<TState>(value, null));
                Interlocked.Exchange(ref _lastUpdateError, null);
                Interlocked.Exchange(ref _failedUpdateIndex, 0);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                _log.LogError(e, $"{nameof(UpdateAsync)}: Error.");
                next.SetOutput(_isolateUpdateErrors 
                    ? prev.Output 
                    : new Result<TState>(default!, e));
                Interlocked.Exchange(ref _lastUpdateError, e);
                var tryIndex = Interlocked.Increment(ref _failedUpdateIndex) - 1;
                UpdateDelayer.ExtraErrorDelayAsync(e, tryIndex, cancellationToken)
                    .ContinueWith(_ => next.Invalidate(), CancellationToken.None)
                    .Ignore();
            }
            finally {
                BeginTracking(next);
            }
        }
    }

    public class LiveState<TState> : LiveState<Unit, TState>
    {
        public new class Options : LiveState<Unit, TState>.Options
        {
            private Func<ILiveState<TState>, CancellationToken, Task<TState>> _updater = null!;

            public new Func<ILiveState<TState>, CancellationToken, Task<TState>> Updater {
                get => _updater;
                set {
                    _updater = value;
                    base.Updater = (liveState, cancellationToken) => _updater.Invoke(liveState, cancellationToken);
                }
            }
        }

        public LiveState(
            Options options, 
            IUpdateDelayer? updateDelayer = null, 
            ILogger<LiveState<TState>>? log = null) 
            : base(options, updateDelayer, log) 
        { }
    }
}
