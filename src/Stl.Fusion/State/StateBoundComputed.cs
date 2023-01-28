namespace Stl.Fusion;

public class StateBoundComputed<T> : Computed<T>
{
    public State<T> State { get; }

    public StateBoundComputed(
        ComputedOptions options,
        State<T> state, LTag version)
        : base(options, state, version)
        => State = state;

    protected StateBoundComputed(
        ComputedOptions options,
        State<T> state,
        Result<T> output, LTag version, bool isConsistent)
        : base(options, state, output, version, isConsistent)
        => State = state;

    protected override void OnInvalidated()
    {
        State.OnInvalidated(this);
        base.OnInvalidated();
    }
}
