namespace Stl.Fusion.Blazor
{
    public abstract class LiveComponentBase<T> : StatefulComponentBase<T>
    {
        protected new ILiveState<T> State { get; private set; } = null!;

        protected override void OnStateAssigned(IState<T> state)
        {
            State = (ILiveState<T>) state;
            base.OnStateAssigned(state);
        }
    }

    public abstract class LiveComponentBase<T, TOwn> : LiveComponentBase<T>
    {
        protected new ILiveState<T, TOwn> State { get; private set; } = null!;
        protected IMutableState<TOwn> OwnState { get; private set; } = null!;

        protected override void OnStateAssigned(IState<T> state)
        {
            State = (ILiveState<T, TOwn>) state;
            OwnState = State.OwnState;
            base.OnStateAssigned(state);
        }
    }
}
