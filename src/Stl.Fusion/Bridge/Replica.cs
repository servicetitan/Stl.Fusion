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
        bool ApplyUpdate(IComputedReplica<T> origin, LTagged<Result<T>> output, bool isConsistent);
    } 

    public class Replica<T> : AsyncDisposableBase, IReplicaImpl<T>
    {
        protected readonly ReplicaInput Input;
        protected IComputedReplica<T> ComputedField;
        protected TaskCompletionSource<IComputedReplica<T>>? UpdateRequestTcs = null;

        public IReplicator Replicator { get; }
        public Symbol PublisherId => Input.PublisherId;
        public Symbol PublicationId => Input.PublicationId;
        IComputedReplica IReplica.Computed => ComputedField;
        public IComputedReplica<T> Computed => ComputedField;
        public bool IsUpdateRequested => UpdateRequestTcs != null;
        protected object Lock => new object();

        public Replica(
            IReplicator replicator, Symbol publisherId, Symbol publicationId, 
            LTagged<Result<T>> initialOutput, bool isConsistent = true, bool isUpdateRequested = false)
        {
            Replicator = replicator;
            Input = new ReplicaInput(this, publisherId, publicationId);
            if (isUpdateRequested)
                // ReSharper disable once VirtualMemberCallInConstructor
                UpdateRequestTcs = CreateUpdateRequestTcs();
            // ReSharper disable once VirtualMemberCallInConstructor
            ApplyUpdate(null, initialOutput, isConsistent);
        }

        protected virtual IComputedReplica<T> CreateComputedReplica(
            LTagged<Result<T>> initialOutput, bool isConsistent) 
            => new ComputedReplica<T>(Input, initialOutput.Value, initialOutput.LTag, isConsistent);

        async Task<IComputedReplica> IReplica.RequestUpdateAsync(CancellationToken cancellationToken) 
            => await RequestUpdateAsync(cancellationToken);
        public virtual Task<IComputedReplica<T>> RequestUpdateAsync(CancellationToken cancellationToken = default)
        {
            var requestUpdate = false;
            TaskCompletionSource<IComputedReplica<T>>? updateRequestTcs;
            lock (Lock) {
                updateRequestTcs = UpdateRequestTcs ?? CreateUpdateRequestTcs();
                if (updateRequestTcs != UpdateRequestTcs) {
                    requestUpdate = true;
                    UpdateRequestTcs = updateRequestTcs;
                }
            }
            if (requestUpdate)
                Input.ReplicatorImpl.TrySubscribe(this, true);
            return updateRequestTcs.Task.WithFakeCancellation(cancellationToken);
        }

        bool IReplicaImpl<T>.ApplyUpdate(IComputedReplica<T>? expectedComputed, LTagged<Result<T>> output, bool isConsistent) 
            => ApplyUpdate(expectedComputed, output, isConsistent);
        protected virtual bool ApplyUpdate(IComputedReplica<T>? expectedComputed, LTagged<Result<T>> output, bool isConsistent)
        {
            TaskCompletionSource<IComputedReplica<T>>? updateRequestTcs;
            IComputedReplica<T> newComputed;
            lock (Lock) {
                if (ComputedField != expectedComputed)
                    return false;
                (updateRequestTcs, UpdateRequestTcs) = (UpdateRequestTcs, null);
                newComputed = new ComputedReplica<T>(Input, output.Value, output.LTag, isConsistent);
                ComputedField = newComputed;
            }
            try {
                expectedComputed?.Invalidate();
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
            if (input != Input)
                // This "Function" supports just a single input == Input
                throw new ArgumentOutOfRangeException(nameof(input));

            using var contextUseScope = context.Use();
            context = contextUseScope.Context;

            var result = Computed;
            if ((context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result?.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
                return result!;
            }

            if (!result.IsConsistent)
                result = await RequestUpdateAsync(cancellationToken).ConfigureAwait(false);
            context.TryCaptureValue(result);
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
            return result;
        }

        protected async Task<T> InvokeAndStripAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            if (input != Input)
                // This "Function" supports just a single input == Input
                throw new ArgumentOutOfRangeException(nameof(input));

            using var contextUseScope = context.Use();
            context = contextUseScope.Context;

            var result = Computed;
            if ((context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result?.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
                return result.Strip();
            }

            if (!result.IsConsistent)
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
