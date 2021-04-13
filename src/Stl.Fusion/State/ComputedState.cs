using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stl.Fusion
{
    public interface IComputedState : IState, IDisposable
    {
        public new interface IOptions : IState.IOptions
        {
            IUpdateDelayer? UpdateDelayer { get; set; }
            Func<IComputedState, IUpdateDelayer>? UpdateDelayerFactory { get; set; }
            bool DelayFirstUpdate { get; set; }
        }

        IUpdateDelayer UpdateDelayer { get; }
    }

    public interface IComputedState<T> : IState<T>, IComputedState
    { }

    public abstract class ComputedState<T> : State<T>, IComputedState<T>
    {
        public new class Options : State<T>.Options, IComputedState.IOptions
        {
            public IUpdateDelayer? UpdateDelayer { get; set; }
            public Func<IComputedState, IUpdateDelayer>? UpdateDelayerFactory { get; set; }
            public bool DelayFirstUpdate { get; set; } = false;
        }

        private readonly CancellationTokenSource _stopCts;

        protected CancellationToken StopToken { get; }
        protected Func<IComputedState<T>, IUpdateDelayer>? UpdateDelayerFactory { get; }
        protected ILogger Log { get; }

        public IUpdateDelayer UpdateDelayer { get; private set; } = null!;
        public bool DelayFirstUpdate { get; }

        protected ComputedState(IServiceProvider services, bool initialize = true)
            : this(new(), services, initialize) { }
        protected ComputedState(Options options, IServiceProvider services, bool initialize = true)
            : base(options, services, false)
        {
            _stopCts = new CancellationTokenSource();
            StopToken = _stopCts.Token;
            Log = Services.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? NullLogger.Instance;
            if (options.UpdateDelayer != null) {
                if (options.UpdateDelayerFactory != null)
                    throw new ArgumentOutOfRangeException(nameof(options));
                UpdateDelayer = options.UpdateDelayer;
            }
            else if (options.UpdateDelayerFactory != null)
                UpdateDelayerFactory = options.UpdateDelayerFactory;
            else
                UpdateDelayerFactory = state => state.Services.GetRequiredService<IUpdateDelayer>();
            DelayFirstUpdate = options.DelayFirstUpdate;
            // ReSharper disable once VirtualMemberCallInConstructor
            if (initialize) Initialize(options);
        }

        protected override void Initialize(State<T>.Options options)
        {
            if (UpdateDelayer == null!)
                UpdateDelayer = UpdateDelayerFactory!.Invoke(this);
            base.Initialize(options);
            Task.Run(Run, StopToken);
        }

        // ~ComputedState() => Dispose();

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
                    await computed.WhenInvalidated(cancellationToken).ConfigureAwait(false);
                    if (snapshot.UpdateCount != 0 || DelayFirstUpdate)
                        await UpdateDelayer.UpdateDelay(snapshot, cancellationToken).ConfigureAwait(false);
                    if (!snapshot.WhenUpdated().IsCompleted)
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
