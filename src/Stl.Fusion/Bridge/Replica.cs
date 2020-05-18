using System;
using System.Reactive;
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

        Task RequestUpdateAsync(CancellationToken cancellationToken = default);
    }

    public interface IReplica<T> : IReplica
    {
        new IComputedReplica<T> Computed { get; }
        
        new Task RequestUpdateAsync(CancellationToken cancellationToken = default);
    }

    public interface IReplicaImpl : IReplica, IFunction { }
    public interface IReplicaImpl<T> : IReplica<T>, IFunction<ReplicaInput, T>, IReplicaImpl
    {
        bool ChangeState(IComputedReplica<T> expected, LTagged<Result<T>> output, bool isConsistent);
        void CompleteUpdateRequest();
    } 

    public class Replica<T> : AsyncDisposableBase, IReplicaImpl<T>
    {
        protected readonly ReplicaInput Input;
        protected IComputedReplica<T> ComputedField = null!;
        protected volatile TaskCompletionSource<Unit>? UpdateRequestTcs = null;
        protected IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;

        public IReplicator Replicator { get; }
        public Symbol PublisherId => Input.PublisherId;
        public Symbol PublicationId => Input.PublicationId;
        IComputedReplica IReplica.Computed => ComputedField;
        public IComputedReplica<T> Computed => ComputedField;
        public bool IsUpdateRequested => UpdateRequestTcs != null;

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
            ChangeState(null, initialOutput, isConsistent);
        }

        protected virtual IComputedReplica<T> CreateComputedReplica(
            LTagged<Result<T>> initialOutput, bool isConsistent) 
            => new ComputedReplica<T>(Input, initialOutput.Value, initialOutput.LTag, isConsistent);

        Task IReplica.RequestUpdateAsync(CancellationToken cancellationToken) 
            => RequestUpdateAsync(cancellationToken);
        public virtual Task RequestUpdateAsync(CancellationToken cancellationToken = default)
        {
            var updateRequestTcs = UpdateRequestTcs;
            if (updateRequestTcs == null) {
                var newUpdateRequestTcs = CreateUpdateRequestTcs();
                updateRequestTcs = Interlocked.CompareExchange(ref UpdateRequestTcs, newUpdateRequestTcs, null);
                if (updateRequestTcs == null) {
                    updateRequestTcs = newUpdateRequestTcs;
                    Input.ReplicatorImpl.TrySubscribe(this, true);
                }
            }
            return updateRequestTcs.Task.WithFakeCancellation(cancellationToken);
        }

        bool IReplicaImpl<T>.ChangeState(IComputedReplica<T>? expected, LTagged<Result<T>> output, bool isConsistent) 
            => ChangeState(expected, output, isConsistent);
        protected virtual bool ChangeState(IComputedReplica<T>? expected, LTagged<Result<T>> output, bool isConsistent)
        {
            var computed = ComputedField;
            if (computed != expected)
                return false;
            var newComputed = new ComputedReplica<T>(Input, output.Value, output.LTag, isConsistent);
            computed = Interlocked.CompareExchange(ref ComputedField, newComputed, expected);
            if (computed != expected)
                return false;
            expected?.Invalidate();
            return true;
        }

        void IReplicaImpl<T>.CompleteUpdateRequest() 
            => CompleteUpdateRequest();
        protected virtual void CompleteUpdateRequest()
        {
            var updateRequestTcs = Interlocked.Exchange(ref UpdateRequestTcs, null);
            updateRequestTcs?.TrySetResult(default);
        }

        protected virtual TaskCompletionSource<Unit> CreateUpdateRequestTcs() 
            => new TaskCompletionSource<Unit>();

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
                    result.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
                return result!;
            }

            var retryPolicy = ReplicatorImpl.RetryPolicy;
            for (var tryIndex = 0;; tryIndex++) {
                await RequestUpdateAsync(cancellationToken).ConfigureAwait(false);
                result = Computed;
                if (result.IsConsistent)
                    break;
                if (!retryPolicy.MustRetry(result, tryIndex))
                    break;
            }
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
                    result.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
                return result.Strip();
            }

            var retryPolicy = ReplicatorImpl.RetryPolicy;
            for (var tryIndex = 0;; tryIndex++) {
                await RequestUpdateAsync(cancellationToken).ConfigureAwait(false);
                result = Computed;
                if (result.IsConsistent)
                    break;
                if (!retryPolicy.MustRetry(result, tryIndex))
                    break;
            }
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
