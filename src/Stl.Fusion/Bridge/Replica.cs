using System;
using System.Diagnostics;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Internal;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public interface IReplica : IAsyncDisposableWithDisposalState
    {
        IReplicator Replicator { get; }
        Symbol PublisherId { get; }
        Symbol PublicationId { get; }
        ComputedOptions ComputedOptions { get; set; }
        IReplicaComputed Computed { get; }
        bool IsUpdateRequested { get; }
        Exception? UpdateError { get; }

        Task RequestUpdateAsync(CancellationToken cancellationToken = default);
    }

    public interface IReplica<T> : IReplica
    {
        new IReplicaComputed<T> Computed { get; }
    }

    public interface IReplicaImpl : IReplica, IFunction
    {
        void MarkDisposed();
        bool ApplyFailedUpdate(Exception? error, CancellationToken cancellationToken);
    }

    public interface IReplicaImpl<T> : IReplica<T>, IFunction<ReplicaInput, T>, IReplicaImpl
    {
        bool ApplySuccessfulUpdate(LTagged<Result<T>> output, bool isConsistent);
    } 

    public class Replica<T> : AsyncDisposableBase, IReplicaImpl<T>
    {
        private volatile ComputedOptions _computedOptions = ComputedOptions.Default;

        protected readonly ReplicaInput Input;
        protected volatile IReplicaComputed<T> ComputedField = null!;
        protected volatile Exception? UpdateErrorField;
        protected volatile Task<Unit>? UpdateRequestTask;
        protected IReplicatorImpl ReplicatorImpl => (IReplicatorImpl) Replicator;
        protected object Lock = new object();

        public IReplicator Replicator { get; }
        public Symbol PublisherId => Input.PublisherId;
        public Symbol PublicationId => Input.PublicationId;
        public ComputedOptions ComputedOptions {
            get => _computedOptions; 
            set => _computedOptions = value;
        }
        IReplicaComputed IReplica.Computed => ComputedField;
        public IReplicaComputed<T> Computed => ComputedField;
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

        // This method is called for temp. replicas that were never attached to anything.
        void IReplicaImpl.MarkDisposed() => MarkDisposed();

        // We want to make sure the replicas are connected to
        // publishers only while they're used.
        ~Replica() => DisposeAsync(false);

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            // Debug.WriteLine($"{nameof(DisposeInternalAsync)}({disposing}) for {PublicationId} / {GetHashCode()}");
            Input.ReplicatorImpl.OnReplicaDisposed(this);
            return base.DisposeInternalAsync(disposing);
        }

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
            IReplicaComputed<T> computed;
            Task<Unit>? updateRequestTask;
            var mustInvalidate = true;
            lock (Lock) {
                // 1. Update Computed & UpdateError 
                UpdateErrorField = null;
                computed = ComputedField;
                if (computed == null || computed.LTag != output.LTag)
                    // LTag doesn't match -> replace
                    ComputedField = new ReplicaComputed<T>(
                        ComputedOptions, Input, output.Value, output.LTag, isConsistent);
                else if (computed.IsConsistent != isConsistent) {
                    // LTag matches:
                    if (isConsistent)
                        // Replace inconsistent w/ the consistent
                        ComputedField = new ReplicaComputed<T>(
                            ComputedOptions, Input, output.Value, output.LTag, isConsistent);
                    // Otherwise it will be invalidated right after exiting the lock 
                }
                else {
                    // Nothing has changed
                    mustInvalidate = false;
                }

                // 2. Complete UpdateRequestTask
                (updateRequestTask, UpdateRequestTask) = (UpdateRequestTask, null);
            }

            // We always invalidate the old computed here, b/c it was either
            // replaced or has to be invalidated.
            if (mustInvalidate)
                computed?.Invalidate();
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
            IReplicaComputed<T>? computed;
            Task<Unit>? updateRequestTask;
            lock (Lock) {
                // 1. Update Computed & UpdateError 
                computed = ComputedField;
                UpdateErrorField = error;

                // 2. Complete UpdateRequestTask
                (updateRequestTask, UpdateRequestTask) = (UpdateRequestTask, null);
            }

            if (error != null)
                computed.Invalidate();
            if (updateRequestTask != null) {
                var result = new Result<Unit>(default, error);
                var updateRequestTaskSource = TaskSource.For(updateRequestTask);
                updateRequestTaskSource.TrySetFromResult(result, cancellationToken);
            }
            return true;
        }

        protected virtual TaskSource<Unit> CreateUpdateRequestTaskSource() 
            => TaskSource.New<Unit>(true);

        protected async Task<IComputed<T>> InvokeAsync(ReplicaInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            if (input != Input)
                // This "Function" supports just a single input == Input
                throw new ArgumentOutOfRangeException(nameof(input));

            using var contextUseScope = context.Use();
            context = contextUseScope.Context;

            var result = Computed;
            var resultIsConsistent = result.IsConsistent;
            if (resultIsConsistent || (context.CallOptions & CallOptions.TryGetCached) != 0) {
                if ((context.CallOptions & CallOptions.Invalidate) == CallOptions.Invalidate)
                    result.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
                context.TryCaptureValue(result);
                return result!;
            }

            // No async locking here b/c RequestUpdateAsync is, in fact, doing this
            await RequestUpdateAsync(cancellationToken).ConfigureAwait(false);
            result = Computed;
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
            context.TryCaptureValue(result);
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
            var resultIsConsistent = result.IsConsistent;
            if (resultIsConsistent || (context.CallOptions & CallOptions.TryGetCached) != 0) {
                if ((context.CallOptions & CallOptions.Invalidate) == CallOptions.Invalidate)
                    result.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
                context.TryCaptureValue(result);
                return result.Strip();
            }

            // No async locking here b/c RequestUpdateAsync is, in fact, doing this
            await RequestUpdateAsync(cancellationToken).ConfigureAwait(false);
            result = Computed;
            ((IComputedImpl?) usedBy)?.AddUsed((IComputedImpl) result);
            context.TryCaptureValue(result);
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
