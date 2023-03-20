namespace Stl.Interception;

public class Interceptor
{
    public T AttachTo<T>(T proxy)
    {
        ((IProxy)proxy!).Bind(this);
        return proxy;
    }

    public virtual TResult Intercept<TResult>(Invocation invocation)
        => invocation.Intercepted<TResult>();

    public virtual void Intercept(Invocation invocation)
        => invocation.Intercepted();
}
