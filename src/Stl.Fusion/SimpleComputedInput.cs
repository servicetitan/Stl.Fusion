using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Concurrency;
using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public abstract class SimpleComputedInput : ComputedInput, 
        IEquatable<SimpleComputedInput>, IFunction
    {
        protected SimpleComputedInput()
        {
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
#pragma warning disable 659
        public override bool Equals(object? obj) 
            => ReferenceEquals(this, obj);
#pragma warning restore 659
    }

    public class SimpleComputedInput<T> : SimpleComputedInput, IFunction<SimpleComputedInput, T>
    {
        public SimpleComputed<T> Computed { get; set; } = null!;
        public Func<SimpleComputed<T>, Task<T>> Updater { get; }

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

            await UpdateAsync().ConfigureAwait(false);
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

            await UpdateAsync().ConfigureAwait(false);
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

        protected virtual async ValueTask UpdateAsync()
        {
            var lTagGenerator = ConcurrentIdGenerator.DefaultLTag;
            try {
                var result = await Updater.Invoke(Computed).ConfigureAwait(false);
                Computed = new SimpleComputed<T>(this, new Result<T>(result, null), lTagGenerator.Next());
            }
            catch (Exception e) {
                Computed = new SimpleComputed<T>(this, new Result<T>(default!, e), lTagGenerator.Next());
            }
        }
    }
}
