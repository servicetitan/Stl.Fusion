using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.DependencyInjection;
using Stl.Internal;
using Stl.Reflection;

namespace Stl.Fusion.UI
{
    public interface ILiveState : IResult, IHasServiceProvider, IDisposable
    {
        IComputed State { get; }
        Exception? UpdateError { get; }
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
            public ComputedOptions StateOptions { get; set; } = ComputedOptions.Default;
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
        private readonly CancellationTokenSource _stopCts;
        private readonly CancellationToken _stopToken;
        private readonly StandaloneComputedInput<TState> _computedRef;
        private volatile Box<TLocal> _local;
        private volatile Exception? _updateError;
        private volatile int _failedUpdateIndex;

        public IServiceProvider ServiceProvider { get; }
        public TLocal Local {
            get => _local.Value;
            set { _local = Box.New(value); Invalidate(); }
        }
        IComputed ILiveState.State => State;
        public IComputed<TState> State => _computedRef!.Computed;
        public Exception? UpdateError => _updateError;
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
            IServiceProvider? serviceProvider = null,
            IUpdateDelayer? updateDelayer = null,
            ILogger<LiveState<TState>>? log = null)
        {
            _log = log ??= NullLogger<LiveState<TState>>.Instance;
            ServiceProvider = serviceProvider ??= ServiceProviderEx.Empty;

            _updater = options.Updater
                ?? throw new ArgumentNullException(nameof(options) + "." + nameof(options.Updater));
            _delayFirstUpdate = options.DelayFirstUpdate;
            _isolateUpdateErrors = options.IsolateUpdateErrors;
            UpdateDelayer = options.UpdateDelayer
                ?? updateDelayer
                ?? throw new ArgumentNullException(nameof(updateDelayer));

            _stopCts = new CancellationTokenSource();
            _stopToken = _stopCts.Token;
            _local = Box.New(options.InitialLocal);
            var computed = (StandaloneComputed<TState>) Computed.New(
                ServiceProvider, options.StateOptions,
                (ComputedUpdater<TState>) UpdateAsync, options.InitialState);
            _computedRef = computed.Input;
            Task.Run(RunAsync);
        }

        ~LiveState() => Dispose();

        public void Dispose()
        {
            if (_stopCts.IsCancellationRequested)
                return;
            GC.SuppressFinalize(this);
            try {
                _stopCts.Cancel();
            }
            finally {
                _stopCts.Dispose();
            }
        }

        public void Deconstruct(out TState value, out Exception? error)
            => State.Deconstruct(out value, out error);

        public bool IsValue([MaybeNullWhen(false)] out TState value)
            => State.IsValue(out value);
        public bool IsValue([MaybeNullWhen(false)] out TState value, [MaybeNullWhen(true)] out Exception error)
            => State.IsValue(out value, out error!);

        public Result<TState> AsResult()
            => State.AsResult();
        public Result<TOther> AsResult<TOther>()
            => State.AsResult<TOther>();

        public void ThrowIfError() => State.ThrowIfError();

        public void Invalidate(bool updateImmediately = true)
        {
            State.Invalidate();
            if (updateImmediately)
                UpdateDelayer.CancelDelays();
        }

        protected virtual async Task RunAsync()
        {
            var cancellationToken = _stopToken;
            var computed = _computedRef.Computed;
            for (var updateIndex = 0;; updateIndex++) {
                await computed.WhenInvalidatedAsync(cancellationToken).ConfigureAwait(false);
                try {
                    if (updateIndex != 0 || _delayFirstUpdate)
                        await UpdateDelayer.DelayAsync(cancellationToken).ConfigureAwait(false);
                    computed = (StandaloneComputed<TState>)
                        await computed.UpdateAsync(false, cancellationToken).ConfigureAwait(false);
                }
                finally {
                    cancellationToken.ThrowIfCancellationRequested();
                    Updated?.Invoke(this);
                }
            }
        }

        private async Task UpdateAsync(
            IComputed<TState> prev, IComputed<TState> next,
            CancellationToken cancellationToken)
        {
            try {
                var value = await _updater.Invoke(this, cancellationToken)
                    .ConfigureAwait(false);
                next.SetOutput(new Result<TState>(value, null));
                Interlocked.Exchange(ref _updateError, null);
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
                Interlocked.Exchange(ref _updateError, e);
                var tryIndex = Interlocked.Increment(ref _failedUpdateIndex);
                UpdateDelayer.ExtraErrorDelayAsync(e, tryIndex, cancellationToken)
                    .ContinueWith(_ => next.Invalidate(), CancellationToken.None)
                    .Ignore();
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
            IServiceProvider? serviceProvider = null,
            IUpdateDelayer? updateDelayer = null,
            ILogger<LiveState<TState>>? log = null)
            : base(options, serviceProvider, updateDelayer, log)
        { }
    }
}
