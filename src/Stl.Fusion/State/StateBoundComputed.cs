namespace Stl.Fusion;

public interface IStateBoundComputed : IComputed
{
    public IState State { get; }
}

public class StateBoundComputed<T> : Computed<T>, IStateBoundComputed
{
    IState IStateBoundComputed.State => State;
    public State<T> State { get; }

    public StateBoundComputed(
        ComputedOptions options,
        State<T> state, LTag version)
        : base(options, state, version)
    {
        State = state;
        ComputedRegistry.Instance.PseudoRegister(this);
    }

    protected StateBoundComputed(
        ComputedOptions options,
        State<T> state,
        Result<T> output,
        LTag version,
        bool isConsistent)
        : base(options, state, output, version, isConsistent)
    {
        State = state;
        if (isConsistent)
            ComputedRegistry.Instance.PseudoRegister(this);
    }

    protected override void OnInvalidated()
    {
        ComputedRegistry.Instance.PseudoUnregister(this);
        CancelTimeouts();
        State.OnInvalidated(this);
    }
}
