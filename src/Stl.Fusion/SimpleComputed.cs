using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;

namespace Stl.Fusion
{
    public class SimpleComputed<T> : Computed<SimpleComputedInput, T>
    {
        public new SimpleComputedInput<T> Input => (SimpleComputedInput<T>) base.Input;

        public SimpleComputed(ComputedOptions options, SimpleComputedInput input, LTag lTag) 
            : base(options, input, lTag) { }
        public SimpleComputed(ComputedOptions options, SimpleComputedInput input, 
            Result<T> output, LTag lTag, bool isConsistent = true) 
            : base(options, input, output, lTag, isConsistent) { }
    }

    public static class SimpleComputed
    {
        // Task versions

        public static SimpleComputed<T> New<T>(
            Func<IComputed<T>, IComputed<T>, CancellationToken, Task> updater)
            => New(ComputedOptions.Default, updater, default, false);
        public static SimpleComputed<T> New<T>(
            ComputedOptions options,
            Func<IComputed<T>, IComputed<T>, CancellationToken, Task> updater)
            => New(options, updater, default, false);

        public static SimpleComputed<T> New<T>(
            Func<IComputed<T>, IComputed<T>, CancellationToken, Task> updater,
            Result<T> output, bool isConsistent = true)
            => New(ComputedOptions.Default, updater, output, isConsistent);
        public static SimpleComputed<T> New<T>(
            ComputedOptions options,
            Func<IComputed<T>, IComputed<T>, CancellationToken, Task> updater,
            Result<T> output, bool isConsistent = true)
        {
            var input = new SimpleComputedInput<T>(updater);
            var lTag = ConcurrentIdGenerator.DefaultLTag.Next();
            input.Computed = new SimpleComputed<T>(options, input, output, lTag, isConsistent);
            return input.Computed;
        }

        public static ValueTask<SimpleComputed<T>> NewAsync<T>(
            Func<IComputed<T>, IComputed<T>, CancellationToken, Task> updater,
            CancellationToken cancellationToken)
            => NewAsync(ComputedOptions.Default, updater, cancellationToken); 
        public static async ValueTask<SimpleComputed<T>> NewAsync<T>(
            ComputedOptions options,
            Func<IComputed<T>, IComputed<T>, CancellationToken, Task> updater,
            CancellationToken cancellationToken)
        {
            var computed = New(options, updater);
            var updated = await computed.UpdateAsync(cancellationToken).ConfigureAwait(false);
            return (SimpleComputed<T>) updated;
        }

        // Task<T> versions

        public static SimpleComputed<T> New<T>(
            Func<IComputed<T>, CancellationToken, Task<T>> updater)
            => New(ComputedOptions.Default, Wrap(updater), default, false);
        public static SimpleComputed<T> New<T>(
            ComputedOptions options,
            Func<IComputed<T>, CancellationToken, Task<T>> updater)
            => New(options, Wrap(updater), default, false);

        public static SimpleComputed<T> New<T>(
            Func<IComputed<T>, CancellationToken, Task<T>> updater,
            Result<T> output, bool isConsistent = true)
            => New(ComputedOptions.Default, Wrap(updater), output, isConsistent);
        public static SimpleComputed<T> New<T>(
            ComputedOptions options,
            Func<IComputed<T>, CancellationToken, Task<T>> updater,
            Result<T> output, bool isConsistent = true)
            => New(options, Wrap(updater), output, isConsistent);

        public static ValueTask<SimpleComputed<T>> NewAsync<T>(
            Func<IComputed<T>, CancellationToken, Task<T>> updater,
            CancellationToken cancellationToken)
            => NewAsync(ComputedOptions.Default, Wrap(updater), cancellationToken);
        public static ValueTask<SimpleComputed<T>> NewAsync<T>(
            ComputedOptions options,
            Func<IComputed<T>, CancellationToken, Task<T>> updater,
            CancellationToken cancellationToken)
            => NewAsync(options, Wrap(updater), cancellationToken);

        // Private methods

        private static Func<IComputed<T>, IComputed<T>, CancellationToken, Task> Wrap<T>(
            Func<IComputed<T>, CancellationToken, Task<T>> updater) 
            => async (prevComputed, nextComputed, cancellationToken) => {
                var value = await updater.Invoke(prevComputed, cancellationToken);
                nextComputed.TrySetOutput(new Result<T>(value, null));
            };
    }
}
