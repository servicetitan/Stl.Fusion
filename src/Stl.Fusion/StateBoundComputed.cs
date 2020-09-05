namespace Stl.Fusion
{
    public class StateBoundComputed<T> : Computed<State<T>, T>
    {
        public StateBoundComputed(
            ComputedOptions options,
            State<T> input, LTag version)
            : base(options, input, version)
        { }

        protected StateBoundComputed(
            ComputedOptions options,
            State<T> input,
            Result<T> output, LTag version, bool isConsistent)
            : base(options, input, output, version, isConsistent)
        { }

        protected override void OnInvalidated()
        {
            try {
                Input.OnInvalidated(this);
            }
            catch {
                // Intended: shouldn't throw errors
            }
        }
    }
}
