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
        protected AsyncLock AsyncLock { get; set; }

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

        public Func<SimpleComputed<T>, Task<T>> Updater { get; }
        public SimpleComputed<T> Computed {
            get => ComputedField;
            set {
                var oldComputed = Interlocked.Exchange(ref ComputedField, value);
                oldComputed?.Invalidate();
            }
        }

        public SimpleComputedInput(Func<SimpleComputed<T>, Task<T>> updater) 
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
            if ((context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result.Invalidate(context.InvalidatedBy);
                ((IComputedImpl?) usedBy)?.AddUsed(result);
                return result!;
            }

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            if ((context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result.Invalidate(context.InvalidatedBy);
                ((IComputedImpl?) usedBy)?.AddUsed(result);
                return result!;
            }

            result = await UpdateAsync().ConfigureAwait(false);
            context.TryCaptureValue(result);
            ((IComputedImpl?) usedBy)?.AddUsed(result);
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
            if ((context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result.Invalidate(context.InvalidatedBy);
                ((IComputedImpl?) usedBy)?.AddUsed(result);
                return result.Strip();
            }

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            if ((context.Options & ComputeOptions.TryGetCached) != 0) {
                context.TryCaptureValue(result);
                if ((context.Options & ComputeOptions.Invalidate) == ComputeOptions.Invalidate)
                    result.Invalidate(context.InvalidatedBy);
                ((IComputedImpl?) usedBy)?.AddUsed(result);
                return result.Strip();
            }

            result = await UpdateAsync().ConfigureAwait(false);
            context.TryCaptureValue(result);
            ((IComputedImpl?) usedBy)?.AddUsed(result);
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

        protected virtual async ValueTask<SimpleComputed<T>> UpdateAsync()
        {
            var lTagGenerator = ConcurrentIdGenerator.DefaultLTag;
            try {
                var result = await Updater.Invoke(Computed).ConfigureAwait(false);
                return Computed = new SimpleComputed<T>(this, new Result<T>(result, null), lTagGenerator.Next());
            }
            catch (Exception e) {
                return Computed = new SimpleComputed<T>(this, new Result<T>(default!, e), lTagGenerator.Next());
            }
        }
    }
}
