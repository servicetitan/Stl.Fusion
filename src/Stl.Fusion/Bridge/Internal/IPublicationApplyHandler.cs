namespace Stl.Fusion.Bridge.Internal
{
    public interface IPublicationApplyHandler<in TArg, out TResult>
    {
        TResult Apply<T>(IPublication<T> publication, TArg arg);
    }
}
