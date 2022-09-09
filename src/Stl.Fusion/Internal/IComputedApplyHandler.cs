namespace Stl.Fusion.Internal;

public interface IComputedApplyHandler<in TArg, out TResult>
{
    TResult Apply<T>(Computed<T> computed, TArg arg);
}
