using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Concurrency;

namespace Stl.Fusion
{
    public class SimpleComputed<T> : Computed<SimpleComputedInput, T>
    {
        public new SimpleComputedInput<T> Input => (SimpleComputedInput<T>) base.Input;

        public SimpleComputed(SimpleComputedInput input, LTag lTag) 
            : base(input, lTag) { }
        public SimpleComputed(SimpleComputedInput input, Result<T> output, LTag lTag, bool isConsistent = true) 
            : base(input, output, lTag, isConsistent) { }
    }

    public static class SimpleComputed
    {
        public static SimpleComputed<T> New<T>(Func<SimpleComputed<T>, Task<T>> updater)
            => New(updater, default, false);

        public static SimpleComputed<T> New<T>(
            Func<SimpleComputed<T>, Task<T>> updater,
            Result<T> output, bool isConsistent = true)
        {
            var input = new SimpleComputedInput<T>(updater);
            var lTag = ConcurrentIdGenerator.DefaultLTag.Next();
            input.Computed = new SimpleComputed<T>(input, output, lTag, isConsistent);
            return input.Computed;
        }

        public static async ValueTask<SimpleComputed<T>> NewAsync<T>(
            Func<SimpleComputed<T>, Task<T>> updater,
            CancellationToken cancellationToken)
        {
            var computed = New(updater);
            var updated = await computed.UpdateAsync(cancellationToken).ConfigureAwait(false);
            return (SimpleComputed<T>) updated;
        }
    }
}
