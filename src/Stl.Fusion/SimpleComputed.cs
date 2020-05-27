using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion
{
    public class SimpleComputed<T> : Computed<SimpleComputedInput, T>
    {
        public async ValueTask<SimpleComputed<T>> NewAsync(
            Func<SimpleComputed<T>, Task<T>> updater,
            CancellationToken cancellationToken)
        {
            var input = new SimpleComputedInput<T>(updater);
            var computed = new SimpleComputed<T>(input, default);
            var result = await computed.UpdateAsync(cancellationToken).ConfigureAwait(false);
            return (SimpleComputed<T>) result;
        }

        public SimpleComputed(SimpleComputedInput input, LTag lTag) : base(input, lTag) { }
        public SimpleComputed(SimpleComputedInput input, Result<T> output, LTag lTag, bool isConsistent = true) : base(input, output, lTag, isConsistent) { }
    }
}
