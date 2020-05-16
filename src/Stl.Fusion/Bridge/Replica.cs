using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Bridge.Internal;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IReplica : IAsyncDisposable
    {
        IReplicator Replicator { get; }
        Symbol PublisherId { get; }
        Symbol PublicationId { get; }
        IComputed Computed { get; }
        Task<IComputed> NextUpdateTask { get; }
    }

    public interface IReplica<T> : IReplica
    {
        new IComputed<T> Computed { get; }
        bool Update(IComputed<T> origin, TaggedResult<T> taggerOutput);
    }

    public interface IReplicaImpl : IReplica, IFunction { }
    public interface IReplicaImpl<T> : IReplica<T>, IFunction<ReplicaInput, T>, IReplicaImpl { } 

    public class Replica<T> : AsyncDisposableBase, IReplicaImpl<T>
    {
        private volatile IComputed<T> _computed;
        protected volatile TaskCompletionSource<IComputed> NextUpdateTcs;
        protected readonly ReplicaInput Input;

        public IReplicator Replicator { get; }
        public Symbol PublisherId { get; }
        public Symbol PublicationId { get; }
        public IComputed<T> Computed => _computed;
        IComputed IReplica.Computed => _computed;
        public Task<IComputed> NextUpdateTask => NextUpdateTcs.Task;
        protected object Lock => this;

        public Replica(IReplicator replicator, 
            Symbol publisherId, Symbol publicationId, 
            TaggedResult<T> initialOutput)
        {
            Replicator = replicator;
            PublisherId = publisherId;
            PublicationId = publicationId;
            Input = new ReplicaInput(this);
            NextUpdateTcs = CreateNextUpdateTcs();
            _computed = new Computed<ReplicaInput, T>(Input, initialOutput.Result, initialOutput.Tag);
        }

        public bool Update(IComputed<T> origin, TaggedResult<T> newOutput)
        {
            var spinWait = new SpinWait();
            while (_computed == origin) {
                var newComputed = new Computed<ReplicaInput, T>(Input, newOutput.Result, newOutput.Tag);
                if (origin == Interlocked.CompareExchange(ref _computed, newComputed, origin)) {
                    var nextUpdateTcs = Interlocked.Exchange(ref NextUpdateTcs, CreateNextUpdateTcs());
                    try {
                        origin.Invalidate();
                    }
                    finally {
                        nextUpdateTcs.SetResult(newComputed);
                    }
                    return true;
                }
                spinWait.SpinOnce();
            }
            return false;
        }

        #region Explicit impl. of IFunction & IFunction<...>

        async Task<IComputed?> IFunction.InvokeAsync(ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => await InvokeAsync((ReplicaInput) input, usedBy, context, cancellationToken);

        Task IFunction.InvokeAndStripAsync(ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAndStripAsync((ReplicaInput) input, usedBy, context, cancellationToken);

        IComputed? IFunction.TryGetCached(ComputedInput input, IComputed? usedBy) 
            => TryGetCached((ReplicaInput) input, usedBy);

        Task<IComputed<T>?> IFunction<ReplicaInput, T>.InvokeAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAsync(input, usedBy, context, cancellationToken);

        Task<T> IFunction<ReplicaInput, T>.InvokeAndStripAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAndStripAsync(input, usedBy, context, cancellationToken);

        IComputed<T>? IFunction<ReplicaInput, T>.TryGetCached(ReplicaInput input, IComputed? usedBy) 
            => TryGetCached(input, usedBy);

        #endregion

        protected virtual TaskCompletionSource<IComputed> CreateNextUpdateTcs() 
            => new TaskCompletionSource<IComputed>();

        protected Task<IComputed<T>?> InvokeAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            var reproductionImpl = (IReplicaImpl<T>) input.ReplicaImpl; 
            var computed = reproductionImpl.Computed;
            if (computed.IsConsistent)
                return Task.FromResult(computed)!;
            return NextUpdateTask.ContinueWith((_, arg) => {
                var reproductionImpl1 = (IReplicaImpl<T>) arg;
                return reproductionImpl1.Computed;
            }, reproductionImpl, cancellationToken)!;
        }

        protected async Task<T> InvokeAndStripAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            var reproductionImpl = (IReplicaImpl<T>) input.ReplicaImpl; 
            var computed = reproductionImpl.Computed;
            if (computed.IsConsistent)
                return computed.Value;
            await NextUpdateTask;
            return reproductionImpl.Computed.Value;
        }

        protected IComputed<T>? TryGetCached(ReplicaInput input, IComputed? usedBy)
        {
            var reproductionImpl = (IReplicaImpl<T>) input.ReplicaImpl;
            return reproductionImpl.Computed;
        }

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            ((IReplicatorImpl) Replicator).OnReproductionDisposed(this);
            return base.DisposeInternalAsync(disposing);
        }
    }
}
