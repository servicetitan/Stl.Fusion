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
        Task<IComputed> NextUpdateAsync(CancellationToken cancellationToken = default);
    }

    public interface IReplica<T> : IReplica
    {
        new IComputed<T> Computed { get; }
        new Task<IComputed<T>> NextUpdateAsync(CancellationToken cancellationToken = default);
    }

    public interface IReplicaImpl : IReplica, IFunction { }
    public interface IReplicaImpl<T> : IReplica<T>, IFunction<ReplicaInput, T>, IReplicaImpl
    {
        bool Update(IComputed<T> origin, TaggedResult<T> taggedOutput);
    } 

    public class Replica<T> : AsyncDisposableBase, IReplicaImpl<T>
    {
        private volatile IComputed<T> _computed;
        protected volatile TaskCompletionSource<IComputed<T>> NextUpdateTcs;
        protected readonly ReplicaInput Input;

        public IReplicator Replicator { get; }
        public Symbol PublisherId { get; }
        public Symbol PublicationId { get; }
        public IComputed<T> Computed => _computed;
        IComputed IReplica.Computed => _computed;
        protected object Lock => this;

        public Replica(IReplicator replicator, 
            Symbol publisherId, Symbol publicationId, 
            TaggedResult<T> initialOutput,
            bool isConsistent = true)
        {
            Replicator = replicator;
            PublisherId = publisherId;
            PublicationId = publicationId;
            Input = new ReplicaInput(this);
            NextUpdateTcs = CreateNextUpdateTcs();
            _computed = new Computed<ReplicaInput, T>(Input, initialOutput.Result, initialOutput.Tag, isConsistent);
        }

        async Task<IComputed> IReplica.NextUpdateAsync(CancellationToken cancellationToken) 
            => await NextUpdateAsync(cancellationToken);
        public Task<IComputed<T>> NextUpdateAsync(CancellationToken cancellationToken) 
            => NextUpdateTcs.Task.WithFakeCancellation(cancellationToken);

        bool IReplicaImpl<T>.Update(IComputed<T> origin, TaggedResult<T> taggedOutput) 
            => Update(origin, taggedOutput);
        protected virtual bool Update(IComputed<T> origin, TaggedResult<T> taggedOutput)
        {
            var spinWait = new SpinWait();
            while (_computed == origin) {
                var newComputed = new Computed<ReplicaInput, T>(Input, taggedOutput.Result, taggedOutput.Tag);
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

        protected virtual TaskCompletionSource<IComputed<T>> CreateNextUpdateTcs() 
            => new TaskCompletionSource<IComputed<T>>();

        protected Task<IComputed<T>?> InvokeAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            var reproductionImpl = (IReplicaImpl<T>) input.ReplicaImpl; 
            var computed = reproductionImpl.Computed;
            if (computed.IsConsistent)
                return Task.FromResult(computed)!;
            return NextUpdateTcs.Task.ContinueWith((_, arg) => {
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
            await NextUpdateTcs.Task;
            return reproductionImpl.Computed.Value;
        }

        protected IComputed<T>? TryGetCached(ReplicaInput input, IComputed? usedBy)
        {
            var reproductionImpl = (IReplicaImpl<T>) input.ReplicaImpl;
            return reproductionImpl.Computed;
        }

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            ((IReplicatorImpl) Replicator).OnReplicaDisposed(this);
            return base.DisposeInternalAsync(disposing);
        }
    }
}
