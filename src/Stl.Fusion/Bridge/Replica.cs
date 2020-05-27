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
        Exception? UpdateError { get; }

        Task RequestUpdateAsync(CancellationToken cancellationToken = default);
    }

    public interface IReplica<T> : IReplica
    {
        new IComputedReplica<T> Computed { get; }
    }

    public interface IReplicaImpl : IReplica, IFunction
    {
        bool ApplyFailedUpdate(Exception? error, CancellationToken cancellationToken);
    }

    public interface IReplicaImpl<T> : IReplica<T>, IFunction<ReplicaInput, T>, IReplicaImpl
    {
        bool ApplySuccessfulUpdate(LTagged<Result<T>> output, bool isConsistent);
    } 

    public class Replica<T> : AsyncDisposableBase, IReplicaImpl<T>
    {
        protected readonly ReplicaInput Input;
        protected volatile IComputedReplica<T> ComputedField = null!;
        protected volatile Exception? UpdateErrorField;
        protected volatile Task<Unit>? UpdateRequestTask;
        protected IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;
        protected object Lock => new object();

        public IReplicator Replicator { get; }
        public Symbol PublisherId => Input.PublisherId;
        public Symbol PublicationId => Input.PublicationId;
        IComputedReplica IReplica.Computed => ComputedField;
        public IComputedReplica<T> Computed => ComputedField;
        public bool IsUpdateRequested => UpdateRequestTask != null;
        public Exception? UpdateError => UpdateErrorField;

        public Replica(
            IReplicator replicator, Symbol publisherId, Symbol publicationId, 
            LTagged<Result<T>> initialOutput, bool isConsistent = true, bool isUpdateRequested = false)
        {
            Replicator = replicator;
            Input = new ReplicaInput(this, publisherId, publicationId);
            // ReSharper disable once VirtualMemberCallInConstructor
            ApplySuccessfulUpdate(initialOutput, isConsistent);
            if (isUpdateRequested)
                // ReSharper disable once VirtualMemberCallInConstructor
                UpdateRequestTask = CreateUpdateRequestTaskSource().Task;
        }

        // We want to make sure the replicas are connected to
        // publishers only while they're used.
        ~Replica() => DisposeAsync(false);

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            Input.ReplicatorImpl.OnReplicaDisposed(this);
            return base.DisposeInternalAsync(disposing);
        }

        protected virtual IComputedReplica<T> CreateComputedReplica(
            LTagged<Result<T>> initialOutput, bool isConsistent) 
            => new ComputedReplica<T>(Input, initialOutput.Value, initialOutput.LTag, isConsistent);

        Task IReplica.RequestUpdateAsync(CancellationToken cancellationToken) 
            => RequestUpdateAsync(cancellationToken);
        public virtual Task RequestUpdateAsync(CancellationToken cancellationToken = default)
        {
            var updateRequestTask = UpdateRequestTask;
            if (updateRequestTask != null)
                return updateRequestTask.WithFakeCancellation(cancellationToken);
            // Double check locking
            lock (Lock) {
                updateRequestTask = UpdateRequestTask;
                if (updateRequestTask != null)
                    return updateRequestTask.WithFakeCancellation(cancellationToken);
                UpdateRequestTask = updateRequestTask = CreateUpdateRequestTaskSource().Task;
                Input.ReplicatorImpl.Subscribe(this);
                return updateRequestTask.WithFakeCancellation(cancellationToken);
            }
        }

        bool IReplicaImpl<T>.ApplySuccessfulUpdate(LTagged<Result<T>> output, bool isConsistent) 
            => ApplySuccessfulUpdate(output, isConsistent);
        protected virtual bool ApplySuccessfulUpdate(LTagged<Result<T>> output, bool isConsistent)
        {
            IComputedReplica<T> computed;
            Task<Unit>? updateRequestTask;
            var mustInvalidate = false;
            lock (Lock) {
                // 1. Update Computed & UpdateError 
                UpdateErrorField = null;
                computed = ComputedField;
                if (computed == null || computed.LTag != output.LTag)
                    ComputedField = new ComputedReplica<T>(Input, output.Value, output.LTag, isConsistent);
                else if (computed.IsConsistent != isConsistent) {
                    if (computed.IsConsistent)
                        mustInvalidate = true;
                    else
                        ComputedField = new ComputedReplica<T>(Input, output.Value, output.LTag, isConsistent);
                } 

                // 2. Complete UpdateRequestTask
                (updateRequestTask, UpdateRequestTask) = (UpdateRequestTask, null);
            }

            if (mustInvalidate)
                computed?.Invalidate(this);
            if (updateRequestTask != null) {
                var updateRequestTaskSource = TaskSource.For(updateRequestTask);
                updateRequestTaskSource.TrySetResult(default);
            }
            return true;
        }

        bool IReplicaImpl.ApplyFailedUpdate(Exception? error, CancellationToken cancellationToken)
            => ApplyFailedUpdate(error, cancellationToken);
        protected virtual bool ApplyFailedUpdate(Exception? error, CancellationToken cancellationToken)
        {
            IComputedReplica<T>? computed;
            Task<Unit>? updateRequestTask;
            lock (Lock) {
                // 1. Update Computed & UpdateError 
                computed = ComputedField;
                UpdateErrorField = error;

                // 2. Complete UpdateRequestTask
                (updateRequestTask, UpdateRequestTask) = (UpdateRequestTask, null);
            }

            if (error != null)
                computed.Invalidate(this);
            if (updateRequestTask != null) {
                var result = new Result<Unit>(default, error);
                var updateRequestTaskSource = TaskSource.For(updateRequestTask);
                updateRequestTaskSource.TrySetFromResult(result, cancellationToken);
            }
            return true;
        }

        protected virtual TaskSource<Unit> CreateUpdateRequestTaskSource() 
            => TaskSource.New<Unit>(TaskCreationOptions.None);

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
                    result.Invalidate(context.InvalidatedBy);
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
                return result!;
            }

            // No async locking here b/c RequestUpdateAsync is, in fact, doing this

            var retryPolicy = ReplicatorImpl.RetryPolicy;
            for (var tryIndex = 0;; tryIndex++) {
                try {
                    await RequestUpdateAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    if (cancellationToken.IsCancellationRequested)
                        throw;
                }
                catch {
                    // Intended: if RequestUpdateAsync fails, replica will become
                    // inconsistent; the exception will be saved anyway, so we 
                    // shouldn't throw it. We should try to reprocess the "still inconsistent"
                    // state though.
                }
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
                    result.Invalidate(context.InvalidatedBy);
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
                return result.Strip();
            }

            // No async locking here b/c RequestUpdateAsync is, in fact, doing this

            var retryPolicy = ReplicatorImpl.RetryPolicy;
            for (var tryIndex = 0;; tryIndex++) {
                try {
                    await RequestUpdateAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    if (cancellationToken.IsCancellationRequested)
                        throw;
                }
                catch {
                    // Intended: if RequestUpdateAsync fails, replica will become
                    // inconsistent; the exception will be saved anyway, so we 
                    // shouldn't throw it. We should try to reprocess the "still inconsistent"
                    // state though.
                }
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
    }
}
