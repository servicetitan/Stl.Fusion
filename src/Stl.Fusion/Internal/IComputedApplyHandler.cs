namespace Stl.Fusion.Internal
{
    public interface IComputedApplyHandler<in TArg, out TResult>
    {
        TResult Apply<TIn, TOut>(IComputed<TIn, TOut> computed, TArg arg)
            where TIn : ComputedInput;
    }
}
