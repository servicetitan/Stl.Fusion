namespace Stl.Interception;

public class Interceptor
{
    public void BindTo(object proxy)
        => ((IProxy)proxy).Bind(this);

    public virtual TResult Intercept<TResult>(Invocation invocation)
        => invocation.Intercepted<TResult>();

    public virtual void Intercept(Invocation invocation)
        => invocation.Intercepted();
}
