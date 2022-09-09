namespace Stl.Fusion.Bridge.Internal;

public interface IPublicationApplyHandler<in TArg, out TResult>
{
    TResult Apply<T>(Publication<T> publication, TArg arg);
}
