using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Fusion
{
    public interface ILiveState : IState, IDisposable
    {
        public new interface IOptions : IState.IOptions
        {
            ILiveStateTimer? LiveStateTimer { get; set; }
            Func<ILiveState, ILiveStateTimer>? LiveStateTimerFactory { get; set; }
            bool DelayFirstUpdate { get; set; }
        }

        ILiveStateTimer LiveStateTimer { get; }
    }

    public interface ILiveState<T> : IState<T>, ILiveState
    { }

    public abstract class LiveState<T> : State<T>, ILiveState<T>
    {
        public new class Options : State<T>.Options, ILiveState.IOptions
        {
            public ILiveStateTimer? LiveStateTimer { get; set; }
            public Func<ILiveState, ILiveStateTimer>? LiveStateTimerFactory { get; set; }
            public bool DelayFirstUpdate { get; set; } = false;
        }

        private readonly CancellationTokenSource _stopCts;

        protected CancellationToken StopToken { get; }
        protected Func<ILiveState<T>, ILiveStateTimer>? LiveStateTimerFactory { get; }
        protected ILogger Log { get; }

        public ILiveStateTimer LiveStateTimer { get; private set; } = null!;
        public bool DelayFirstUpdate { get; }

        protected LiveState(IServiceProvider services, bool initialize = true)
            : this(new(), services, initialize) { }
        protected LiveState(Options options, IServiceProvider services, bool initialize = true)
            : base(options, services, false)
        {
            _stopCts = new CancellationTokenSource();
            StopToken = _stopCts.Token;
            Log = Services.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? NullLogger.Instance;
            if (options.LiveStateTimer != null) {
                if (options.LiveStateTimerFactory != null)
                    throw new ArgumentOutOfRangeException(nameof(options));
                LiveStateTimer = options.LiveStateTimer;
            }
            else if (options.LiveStateTimerFactory != null)
                LiveStateTimerFactory = options.LiveStateTimerFactory;
            else
                LiveStateTimerFactory = state => state.Services.GetRequiredService<ILiveStateTimer>();
            DelayFirstUpdate = options.DelayFirstUpdate;
            // ReSharper disable once VirtualMemberCallInConstructor
            if (initialize) Initialize(options);
        }

        protected override void Initialize(State<T>.Options options)
        {
            if (LiveStateTimer == null!)
                LiveStateTimer = LiveStateTimerFactory!.Invoke(this);
            base.Initialize(options);
            Task.Run(Run, StopToken);
        }

        // ~LiveState() => Dispose();

        public virtual void Dispose()
        {
            if (StopToken.IsCancellationRequested)
                return;
            GC.SuppressFinalize(this);
            try {
                _stopCts.Cancel();
            }
            catch {
                // Intended
            }
            finally {
                _stopCts.Dispose();
            }
        }

        protected virtual async Task Run()
        {
            var cancellationToken = StopToken;
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    var snapshot = Snapshot;
                    var computed = snapshot.Computed;
                    var whenUpdatedTask = snapshot.WhenUpdated();
                    await computed.WhenInvalidated(cancellationToken).ConfigureAwait(false);
                    if (snapshot.UpdateCount != 0 || DelayFirstUpdate) {
                        var delayTask = LiveStateTimer.UpdateDelay(snapshot.RetryCount, cancellationToken);
                        await Task.WhenAny(delayTask, whenUpdatedTask).ConfigureAwait(false);
                    }
                    if (!whenUpdatedTask.IsCompleted)
                        await computed.Update(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    // Will break from "while" loop later if it's due to cancellationToken cancellation
                }
                catch (Exception e) {
                    Log.LogError(e, "Failure inside Run()");
                }
            }
        }
    }
}
