using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Fusion.Internal;

namespace Stl.Fusion.UI
{
    public interface ILive : IResult, IDisposable
    {
        IComputed Computed { get; }
        Exception? LastUpdateError { get; }
        IUpdateDelayer UpdateDelayer { get; }
        event Action<ILive>? Updated;

        void Invalidate();
    }

    public interface ILive<T> : ILive, IResult<T>
    {
        new IComputed<T> Computed { get; }

        void Start(Result<T> @default);
    }

    public sealed class Live<T> : ILive<T>
    {
        public sealed class Options
        {
            public Result<T> Default { get; set; } = default;
            public Func<IComputed<T>, CancellationToken, Task<T>> Updater { get; set; } = null!;
            public bool AutoStart { get; set; } = true;
            public bool IsolateUpdateErrors { get; set; } = true;
            public bool DelayFirstUpdate { get; set; } = false;
            public IUpdateDelayer? UpdateDelayer { get; set; } = null;
        }

        private readonly ILogger _log; 
        private readonly Func<IComputed<T>, CancellationToken, Task<T>> _updater;
        private readonly bool _isolateUpdateErrors; 
        private readonly bool _delayFirstUpdate; 
        private readonly Action<IComputed> _invalidationHandler;
        private readonly CancellationTokenSource _disposeCts;
        private readonly CancellationToken _disposeCtsToken;
        private SimpleComputedInput<T>? _computedRef;
        private volatile IComputed<T>? _trackedComputed;
        private volatile Exception? _lastUpdateError;
        private volatile int _failedUpdateIndex;
        private volatile int _updateIndex;

        IComputed ILive.Computed => Computed;
        public IComputed<T> Computed => _computedRef!.Computed;
        public Exception? LastUpdateError => _lastUpdateError;
        public IUpdateDelayer UpdateDelayer { get; }
        public event Action<ILive>? Updated;

        // IResult<T> & IResult
        object? IResult.UnsafeValue => UnsafeValue;
        public T UnsafeValue => Computed.UnsafeValue;
        object? IResult.Value => Value;
        public T Value => Computed.Value;
        public Exception? Error => Computed.Error;
        public bool HasValue => Computed.HasValue;
        public bool HasError => Computed.HasError;

        public Live(
            Options options,
            IUpdateDelayer? updateDelayer = null,
            ILogger<Live<T>>? log = null)
        {
            _log = log ??= NullLogger<Live<T>>.Instance;

            _updater = options.Updater 
                ?? throw new ArgumentNullException(nameof(options) + "." + nameof(options.Updater));
            _delayFirstUpdate = options.DelayFirstUpdate;
            _isolateUpdateErrors = options.IsolateUpdateErrors;
            UpdateDelayer = options.UpdateDelayer ?? updateDelayer ?? UI.UpdateDelayer.Default;

            _invalidationHandler = OnInvalidated;
            _disposeCts = new CancellationTokenSource();
            _disposeCtsToken = _disposeCts.Token;

            if (options.AutoStart)
                Start(options.Default);
        }

        ~Live() => Dispose();

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

        public void Deconstruct(out T value, out Exception? error)
            => Computed.Deconstruct(out value, out error);

        public void ThrowIfError() => Computed.ThrowIfError();
        public bool IsValue([MaybeNullWhen(false)] out T value) 
            => Computed.IsValue(out value);
        public bool IsValue([MaybeNullWhen(false)] out T value, [MaybeNullWhen(true)] out Exception error) 
            => Computed.IsValue(out value, out error!);

        public void Invalidate() => Computed.Invalidate();

        public void Start(Result<T> @default)
        {
            var computed = SimpleComputed.New(Updater, @default, false);
            var oldComputedRef = Interlocked.CompareExchange(
                ref _computedRef, computed.Input, null);
            if (oldComputedRef != null)
                throw Errors.AlreadyStarted();
            BeginTracking(computed);
        }

        private void BeginTracking(IComputed<T>? newComputed)
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

        private async Task Updater(
            IComputed<T> prev, IComputed<T> next, 
            CancellationToken cancellationToken)
        {
            try {
                var value = await _updater.Invoke(prev, cancellationToken)
                    .ConfigureAwait(false);
                next.SetOutput(new Result<T>(value, null));
                Interlocked.Exchange(ref _lastUpdateError, null);
                Interlocked.Exchange(ref _failedUpdateIndex, 0);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                _log.LogError(e, $"{nameof(Updater)}: Error.");
                next.SetOutput(_isolateUpdateErrors 
                    ? prev.Output 
                    : new Result<T>(default!, e));
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
}
