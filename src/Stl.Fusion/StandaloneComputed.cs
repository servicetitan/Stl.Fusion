using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public class StandaloneComputed<T> : Computed<StandaloneComputedInput, T>
    {
        public new StandaloneComputedInput<T> Input => (StandaloneComputedInput<T>) base.Input;

        protected internal StandaloneComputed(ComputedOptions options, StandaloneComputedInput input, LTag version)
            : base(options, input, version)
        {
            if (options.IsAsyncComputed)
                throw Errors.UnsupportedComputedOptions(GetType());
        }

        protected internal StandaloneComputed(
            ComputedOptions options, StandaloneComputedInput input,
            Result<T> output, LTag version, bool isConsistent)
            : base(options, input, output, version, isConsistent)
        {
            if (options.IsAsyncComputed)
                throw Errors.UnsupportedComputedOptions(GetType());
        }
    }
}
