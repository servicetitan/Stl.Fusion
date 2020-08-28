namespace Stl.Fusion.Blazor
{
    public abstract class LiveComponentBase<T> : StatefulComponentBase<ILiveState<T>>
    { }

    public abstract class LiveComponentBase<T, TOwn> : StatefulComponentBase<ILiveState<T, TOwn>>
    {
        protected IMutableState<TOwn> OwnState => State.OwnState;
    }
}
