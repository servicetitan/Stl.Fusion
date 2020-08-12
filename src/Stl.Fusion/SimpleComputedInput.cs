using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Concurrency;
using Stl.Fusion.Internal;
using Stl.Generators;
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

        IComputed? IFunction.TryGetCached(ComputedInput input) => TryGetCached(input);
        protected abstract IComputed? TryGetCached(ComputedInput input);

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

            context ??= ComputeContext.Current;

            var result = Computed;
            if (result.TryUseCached(context, usedBy))
                return result;

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            if (result.TryUseCached(context, usedBy))
                return result;

            result = await ComputeAsync(cancellationToken).ConfigureAwait(false);
            result.UseNew(context, usedBy);
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

            context ??= ComputeContext.Current;

            var result = Computed;
            if (result.TryUseCached(context, usedBy))
                return result.Strip();

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            if (result.TryUseCached(context, usedBy))
                return result.Strip();

            result = await ComputeAsync(cancellationToken).ConfigureAwait(false);
            result.UseNew(context, usedBy);
            return result.Strip();
        }

        IComputed<T>? IFunction<SimpleComputedInput, T>.TryGetCached(SimpleComputedInput input)
            => TryGetCached(input);
        protected override IComputed? TryGetCached(ComputedInput input)
            => TryGetCached((SimpleComputedInput) input);
        protected virtual IComputed<T>? TryGetCached(SimpleComputedInput input)
        {
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));
            return Computed;
        }

        protected virtual async ValueTask<SimpleComputed<T>> ComputeAsync(CancellationToken cancellationToken)
        {
            var oldComputed = Computed;
            var version = ConcurrentLTagGenerator.Default.Next();
            var newComputed = new SimpleComputed<T>(Computed.Options, this, version);
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
