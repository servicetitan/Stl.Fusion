using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Bridge.Internal;
using Stl.Fusion.Internal;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IReplica : IAsyncDisposable
    {
        IReplicator Replicator { get; }
        Symbol PublisherId { get; }
        Symbol PublicationId { get; }
        IComputedReplica Computed { get; }
        bool IsUpdateRequested { get; }

        Task<IComputedReplica> RequestUpdateAsync(CancellationToken cancellationToken = default);
    }

    public interface IReplica<T> : IReplica
    {
        new IComputedReplica<T> Computed { get; }
        
        new Task<IComputedReplica<T>> RequestUpdateAsync(CancellationToken cancellationToken = default);
    }

    public interface IReplicaImpl : IReplica, IFunction { }
    public interface IReplicaImpl<T> : IReplica<T>, IFunction<ReplicaInput, T>, IReplicaImpl
    {
        bool ApplyUpdate(IComputedReplica<T> origin, TaggedResult<T> taggedOutput);
    } 

    public class Replica<T> : AsyncDisposableBase, IReplicaImpl<T>
    {
        protected readonly ReplicaInput Input;
        protected IComputedReplica<T> ComputedField;
        protected TaskCompletionSource<IComputedReplica<T>>? UpdateRequestTcs = null;

        IComputedReplica IReplica.Computed => ComputedField;
        public IComputedReplica<T> Computed => ComputedField;
        public IReplicator Replicator => Input.Replicator;
        public Symbol PublisherId => Input.PublisherId;
        public Symbol PublicationId => Input.PublicationId;
        public bool IsUpdateRequested => UpdateRequestTcs != null;
        protected object Lock => this;

        public Replica(
            IReplicator replicator, Symbol publisherId, Symbol publicationId, 
            TaggedResult<T> initialOutput, bool isConsistent = true, bool isUpdateRequested = false)
        {
            Input = new ReplicaInput(this, publisherId, publicationId);
            ComputedField = new ComputedReplica<T>(Input, initialOutput.Result, initialOutput.Tag, isConsistent);
            if (isUpdateRequested)
                // ReSharper disable once VirtualMemberCallInConstructor
                UpdateRequestTcs = CreateUpdateRequestTcs();
        }

        async Task<IComputedReplica> IReplica.RequestUpdateAsync(CancellationToken cancellationToken) 
            => await RequestUpdateAsync(cancellationToken);
        public virtual Task<IComputedReplica<T>> RequestUpdateAsync(CancellationToken cancellationToken = default)
        {
            var requestUpdate = false;
            TaskCompletionSource<IComputedReplica<T>>? updateRequestTcs;
            lock (Lock) {
                updateRequestTcs = UpdateRequestTcs ?? new TaskCompletionSource<IComputedReplica<T>>();
                if (updateRequestTcs != UpdateRequestTcs) {
                    requestUpdate = true;
                    UpdateRequestTcs = updateRequestTcs;
                }
            }
            if (requestUpdate)
                Input.ReplicatorImpl.TrySubscribe(this, true);
            return updateRequestTcs.Task.WithFakeCancellation(cancellationToken);
        }

        bool IReplicaImpl<T>.ApplyUpdate(IComputedReplica<T> expectedComputed, TaggedResult<T> taggedOutput) 
            => ApplyUpdate(expectedComputed, taggedOutput);
        protected virtual bool ApplyUpdate(IComputedReplica<T> expectedComputed, TaggedResult<T> taggedOutput)
        {
            TaskCompletionSource<IComputedReplica<T>>? updateRequestTcs;
            IComputedReplica<T> newComputed;
            lock (Lock) {
                if (ComputedField != expectedComputed)
                    return false;
                updateRequestTcs = UpdateRequestTcs;
                newComputed = new ComputedReplica<T>(Input, taggedOutput.Result, taggedOutput.Tag);
                ComputedField = newComputed;
            }
            try {
                expectedComputed.Invalidate();
            }
            finally {
                updateRequestTcs?.TrySetResult(newComputed);
            }
            return true;
        }

        protected virtual TaskCompletionSource<IComputedReplica<T>> CreateUpdateRequestTcs() 
            => new TaskCompletionSource<IComputedReplica<T>>();

        protected async Task<IComputed<T>> InvokeAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            using var contextUseScope = context.Use();
            context = contextUseScope.Context;

            var result = TryGetCached(input, usedBy);
            context.TryCaptureValue(result);
            if (result != null || (context.Options & ComputeOptions.TryGetCached) != 0) {
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result?.Invalidate();
                return result!;
            }

            result = await RequestUpdateAsync(cancellationToken).ConfigureAwait(false);
            context.TryCaptureValue(result);
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
            return result;
        }

        protected async Task<T> InvokeAndStripAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            using var contextUseScope = context.Use();
            context = contextUseScope.Context;

            var result = TryGetCached(input, usedBy);
            context.TryCaptureValue(result);
            if (result != null || (context.Options & ComputeOptions.TryGetCached) != 0) {
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result?.Invalidate();
                return result.Strip();
            }

            result = await RequestUpdateAsync(cancellationToken).ConfigureAwait(false);
            context.TryCaptureValue(result);
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
            return result.Strip();
        }

        protected IComputed<T>? TryGetCached(ReplicaInput input, IComputed? usedBy)
        {
            if (input != Input)
                // This "Function" supports just a single input == Input
                throw new ArgumentOutOfRangeException(nameof(input));

            var computed = Computed;
            if (computed != null)
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) computed);
            return computed;
        }

        #region Explicit impl. of IFunction & IFunction<...>

        async Task<IComputed> IFunction.InvokeAsync(ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => await InvokeAsync((ReplicaInput) input, usedBy, context, cancellationToken);

        Task IFunction.InvokeAndStripAsync(ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAndStripAsync((ReplicaInput) input, usedBy, context, cancellationToken);

        IComputed? IFunction.TryGetCached(ComputedInput input, IComputed? usedBy) 
            => TryGetCached((ReplicaInput) input, usedBy);

        Task<IComputed<T>> IFunction<ReplicaInput, T>.InvokeAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAsync(input, usedBy, context, cancellationToken);

        Task<T> IFunction<ReplicaInput, T>.InvokeAndStripAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAndStripAsync(input, usedBy, context, cancellationToken);

        IComputed<T>? IFunction<ReplicaInput, T>.TryGetCached(ReplicaInput input, IComputed? usedBy) 
            => TryGetCached(input, usedBy);

        #endregion

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            Input.ReplicatorImpl.OnReplicaDisposed(this);
            return base.DisposeInternalAsync(disposing);
        }
    }
}
