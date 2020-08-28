using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Fusion
{
    public interface ILiveState : IComputedState
    {
        IUpdateDelayer UpdateDelayer { get; }
        Task? ActiveUpdateTask { get; }
    }

    public interface ILiveState<T> : IComputedState<T>, ILiveState, IDisposable { }

    public interface ILiveState<T, TOwn> : ILiveState<T>
    {
        IMutableState<TOwn> OwnState { get; }
    }

    public abstract class LiveState<T> : ComputedState<T>, ILiveState<T>
    {
        public new class Options : ComputedState<T>.Options
        {
            public static readonly Func<ILiveState<T>, IUpdateDelayer> DefaultUpdateDelayerFactory =
                liveState => {
                    var services = liveState.ServiceProvider;

                    var updateDelayer = services.GetService<IUpdateDelayer<T>>();
                    if (updateDelayer != null)
                        return updateDelayer;

                    var options = services.GetService<UpdateDelayer<T>.Options>();
                    if (options != null)
                        return new UpdateDelayer<T>(options);

                    return services.GetRequiredService<IUpdateDelayer>();
                };

            public Func<ILiveState<T>, IUpdateDelayer> UpdateDelayerFactory { get; set; } = DefaultUpdateDelayerFactory;

            public void NoUpdateDelay()
                => UpdateDelayerFactory = _ => Fusion.UpdateDelayer.None;
        }

        private readonly CancellationTokenSource _disposeCts;
        private volatile Task? _activeUpdateTask;

        protected Func<ILiveState<T>, IUpdateDelayer> UpdateDelayerFactory { get; set; }
        protected CancellationToken DisposeToken { get; }
        protected ILogger Log { get; }
        public IUpdateDelayer UpdateDelayer { get; protected set; } = null!;
        public Task? ActiveUpdateTask => _activeUpdateTask;

        protected LiveState(
            Options options, IServiceProvider serviceProvider,
            object? argument = null, bool initialize = true)
            : base(options, serviceProvider, argument, false)
        {
            _disposeCts = new CancellationTokenSource();
            DisposeToken = _disposeCts.Token;
            Log = ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? NullLogger.Instance;
            UpdateDelayerFactory = options.UpdateDelayerFactory;
            if (initialize) Initialize(options);
        }

        protected override void Initialize(State<T>.Options options)
        {
            UpdateDelayer = UpdateDelayerFactory.Invoke(this);
            base.Initialize(options);
        }

        public virtual void Dispose()
        {
            if (DisposeToken.IsCancellationRequested)
                return;
            try {
                _disposeCts.Cancel();
            }
            catch {
                _disposeCts.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        protected internal override void OnInvalidated()
        {
            if (!DisposeToken.IsCancellationRequested) {
                var newUpdateTask = Task.Run(DelayedUpdateAsync, DisposeToken);
                var oldUpdateTask = Interlocked.Exchange(ref _activeUpdateTask, newUpdateTask);
                if (oldUpdateTask != null)
                    Log.LogError($"{nameof(ActiveUpdateTask)} should be null here.");
            }
            base.OnInvalidated();
        }

        protected override void OnUpdated(IStateSnapshot<T>? oldSnapshot)
        {
            if (oldSnapshot != null) {
                var oldUpdateTask = Interlocked.Exchange(ref _activeUpdateTask, null);
                if (oldUpdateTask == null)
                    Log.LogError($"{nameof(ActiveUpdateTask)} shouldn't be null here.");
                else if (!oldUpdateTask.IsCompleted)
                    Log.LogError($"{nameof(ActiveUpdateTask)} should be completed here.");
            }
            base.OnUpdated(oldSnapshot);
        }

        protected virtual async Task DelayedUpdateAsync()
        {
            await UpdateDelayer.DelayAsync(this, DisposeToken).ConfigureAwait(false);
            DisposeToken.ThrowIfCancellationRequested();
            await Computed.UpdateAsync(false, DisposeToken).ConfigureAwait(false);
        }
    }

    public abstract class LiveState<T, TOwn> : LiveState<T>, ILiveState<T, TOwn>
    {
        public new class Options : LiveState<T>.Options
        {
            public MutableState<TOwn>.Options OwnStateOptions { get; set; } = new MutableState<TOwn>.Options();
            public bool AutoUpdateOnOwnStateChange { get; set; } = true;
        }

        public IMutableState<TOwn> OwnState { get; }

        protected LiveState(
            Options options, IServiceProvider serviceProvider,
            object? argument = null, bool initialize = true)
            : base(options, serviceProvider, argument, false)
        {
            OwnState = new MutableState<TOwn>(options.OwnStateOptions, serviceProvider, default, this);
            if (options.AutoUpdateOnOwnStateChange)
                OwnState.Updated += ownState => {
                    var self = ownState.Argument as LiveState<T, TOwn>;
                    self?.Invalidate(true);
                };
            if (initialize) Initialize(options);
        }
    }
}
