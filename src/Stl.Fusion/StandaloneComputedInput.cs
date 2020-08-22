using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Internal;
using Stl.Generators;
using Stl.Locking;

namespace Stl.Fusion
{
    public abstract class StandaloneComputedInput : ComputedInput,
        IEquatable<StandaloneComputedInput>, IFunction
    {
        public IServiceProvider ServiceProvider { get; }
        public AsyncLock AsyncLock { get; }

        protected StandaloneComputedInput(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
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

        // Equality

        public bool Equals(StandaloneComputedInput? other)
            => ReferenceEquals(this, other);
        public override bool Equals(ComputedInput other)
            => ReferenceEquals(this, other);
        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj);
        public override int GetHashCode()
            => base.GetHashCode();
    }

    public class StandaloneComputedInput<T> : StandaloneComputedInput, IFunction<StandaloneComputedInput, T>
    {
        protected volatile StandaloneComputed<T> ComputedField = null!;

        public ComputedUpdater<T> Updater { get; }
        public StandaloneComputed<T> Computed {
            get => ComputedField;
            set {
                var oldComputed = Interlocked.Exchange(ref ComputedField, value);
                oldComputed?.Invalidate();
            }
        }

        public StandaloneComputedInput(IServiceProvider serviceProvider, ComputedUpdater<T> updater)
            : base(serviceProvider)
            => Updater = updater;

        // IFunction

        Task<IComputed<T>> IFunction<StandaloneComputedInput, T>.InvokeAsync(StandaloneComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken) =>
            InvokeAsync(input, usedBy, context, cancellationToken);

        protected override async Task<IComputed> InvokeAsync(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => await InvokeAsync((StandaloneComputedInput) input, usedBy, context, cancellationToken).ConfigureAwait(false);

        protected virtual async Task<IComputed<T>> InvokeAsync(
            StandaloneComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));

            context ??= ComputeContext.Current;

            var result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result;

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result;

            result = await ComputeAsync(cancellationToken).ConfigureAwait(false);
            result.UseNew(context, usedBy);
            return result;
        }

        Task<T> IFunction<StandaloneComputedInput, T>.InvokeAndStripAsync(
            StandaloneComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => InvokeAndStripAsync(input, usedBy, context, cancellationToken);

        protected override async Task InvokeAndStripAsync(
            ComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
            => await InvokeAndStripAsync((StandaloneComputedInput) input, usedBy, context, cancellationToken).ConfigureAwait(false);

        protected virtual async Task<T> InvokeAndStripAsync(
            StandaloneComputedInput input, IComputed? usedBy, ComputeContext? context,
            CancellationToken cancellationToken)
        {
            if (input != this)
                // This "Function" supports just a single input == this
                throw new ArgumentOutOfRangeException(nameof(input));

            context ??= ComputeContext.Current;

            var result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result.Strip();

            using var _ = await AsyncLock.LockAsync(cancellationToken);

            result = Computed;
            if (result.TryUseExisting(context, usedBy))
                return result.Strip();

            result = await ComputeAsync(cancellationToken).ConfigureAwait(false);
            result.UseNew(context, usedBy);
            return result.Value;
        }

        protected virtual async ValueTask<StandaloneComputed<T>> ComputeAsync(CancellationToken cancellationToken)
        {
            var oldComputed = Computed;
            var version = ConcurrentLTagGenerator.Default.Next();
            var newComputed = new StandaloneComputed<T>(Computed.Options, this, version);
            using var _ = Fusion.Computed.ChangeCurrent(newComputed);
            await Updater.Update(oldComputed, newComputed, cancellationToken).ConfigureAwait(false);
            Computed = newComputed;
            return newComputed;
        }
    }
}
