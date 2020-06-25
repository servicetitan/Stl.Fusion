using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Concurrency;
using Stl.Fusion.Internal;
using Stl.Locking;

namespace Stl.Fusion
{
    public abstract class SimpleComputedInput : ComputedInput, 
        IEquatable<SimpleComputedInput>, IFunction
    {
        public AsyncLock AsyncLock { get; set; }

        protected SimpleComputedInput()
        {
            AsyncLock = new AsyncLock(ReentryMode.CheckedFail);
            Function = this;
            HashCode = RuntimeHelpers.GetHashCode(this);
        }

        public virtual ValueTask DisposeAsync() => ValueTaskEx.CompletedTask;

        public override string ToString() 
            => $"{GetType().Name}(#{HashCode})";

        // IFunction

        Task<IComputed> IFunction.InvokeAsync(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) =>
            InvokeAsync(input, usedBy, context, cancellationToken);
        protected abstract Task<IComputed> InvokeAsync(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken);

        Task IFunction.InvokeAndStripAsync(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) =>
            InvokeAndStripAsync(input, usedBy, context, cancellationToken);
        protected abstract Task InvokeAndStripAsync(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken);

        IComputed? IFunction.TryGetCached(ComputedInput input, IComputed? usedBy) 
            => TryGetCached(input, usedBy);
        protected abstract IComputed? TryGetCached(ComputedInput input, IComputed? usedBy); 

        // Equality

        public bool Equals(SimpleComputedInput? other) 
            => ReferenceEquals(this, other);
        public override bool Equals(ComputedInput other)
            => ReferenceEquals(this, other);
        public override bool Equals(object? obj) 
            => ReferenceEquals(this, obj);
        public override int GetHashCode() 
            => base.GetHashCode();
    }

    public class SimpleComputedInput<T> : SimpleComputedInput, IFunction<SimpleComputedInput, T>
    {
        protected volatile SimpleComputed<T> ComputedField = null!;

        public Func<IComputed<T>, IComputed<T>, CancellationToken, Task> Updater { get; }
        public SimpleComputed<T> Computed {
            get => ComputedField;
            set {
                var oldComputed = Interlocked.Exchange(ref ComputedField, value);
                oldComputed?.Invalidate();
            }
        }

        public SimpleComputedInput(Func<IComputed<T>, IComputed<T>, CancellationToken, Task> updater) 
            => Updater = updater;

        // IFunction

        Task<IComputed<T>> IFunction<SimpleComputedInput, T>.InvokeAsync(SimpleComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) =>
            InvokeAsync(input, usedBy, context, cancellationToken);

        protected override async Task<IComputed> InvokeAsync(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => await InvokeAsync((SimpleComputedInput) input, usedBy, context, cancellationToken).ConfigureAwait(false);

        protected virtual async Task<IComputed<T>> InvokeAsync(
            SimpleComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));

            using var contextUseScope = context.Use();
            context = contextUseScope.Context;

            var result = Computed;
            var resultIsConsistent = result.IsConsistent;
            if (resultIsConsistent || (context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed(result);
                return result!;
            }

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            resultIsConsistent = result.IsConsistent;
            if (resultIsConsistent || (context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed(result);
                return result!;
            }

            result = await ComputeAsync(cancellationToken).ConfigureAwait(false);
            ((IComputedImpl?) usedBy)?.AddUsed(result);
            context.TryCaptureValue(result);
            return result;
        }

        Task<T> IFunction<SimpleComputedInput, T>.InvokeAndStripAsync(
            SimpleComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => InvokeAndStripAsync(input, usedBy, context, cancellationToken);

        protected override async Task InvokeAndStripAsync(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) 
            => await InvokeAndStripAsync((SimpleComputedInput) input, usedBy, context, cancellationToken).ConfigureAwait(false);

        protected virtual async Task<T> InvokeAndStripAsync(
            SimpleComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));

            using var contextUseScope = context.Use();
            context = contextUseScope.Context;

            var result = Computed;
            var resultIsConsistent = result.IsConsistent;
            if (resultIsConsistent || (context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed(result);
                return result.Strip();
            }

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            resultIsConsistent = result.IsConsistent;
            if (resultIsConsistent || (context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result.Invalidate();
                ((IComputedImpl?) usedBy)?.AddUsed(result);
                return result.Strip();
            }

            result = await ComputeAsync(cancellationToken).ConfigureAwait(false);
            ((IComputedImpl?) usedBy)?.AddUsed(result);
            context.TryCaptureValue(result);
            return result.Strip();
        }

        IComputed<T>? IFunction<SimpleComputedInput, T>.TryGetCached(SimpleComputedInput input, IComputed? usedBy) 
            => TryGetCached(input, usedBy);

        protected override IComputed? TryGetCached(ComputedInput input, IComputed? usedBy) 
            => TryGetCached((SimpleComputedInput) input, usedBy);

        protected virtual IComputed<T>? TryGetCached(SimpleComputedInput input, IComputed? usedBy)
        {
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));

            var computed = Computed;
            if (computed != null)
                ((IComputedImpl?) usedBy)?.AddUsed(computed);
            return computed;
        }

        protected virtual async ValueTask<SimpleComputed<T>> ComputeAsync(CancellationToken cancellationToken)
        {
            var oldComputed = Computed;
            var lTag = ConcurrentIdGenerator.DefaultLTag.Next();
            var newComputed = new SimpleComputed<T>(this, lTag);
            try {
                using var _ = Fusion.Computed.ChangeCurrent(newComputed);
                await Updater.Invoke(oldComputed, newComputed, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                newComputed.TrySetOutput(new Result<T>(default!, e));
            }
            Computed = newComputed;
            return newComputed;
        }
    }
}
