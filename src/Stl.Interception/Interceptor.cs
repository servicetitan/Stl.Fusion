namespace Stl.Interception;

public class Interceptor
{
    public void BindTo(object proxy)
        => ((IProxy)proxy).Bind(this);

    public virtual TResult Intercept<TResult>(Invocation invocation)
    {
        return invocation.Delegate is Func<ArgumentList, TResult> func
            ? func.Invoke(invocation.Arguments)
#pragma warning disable MA0025
            : throw new NotImplementedException();
#pragma warning restore MA0025
    }

    public virtual void Intercept(Invocation invocation)
    {
        if (invocation.Delegate is Action<ArgumentList> action)
            action.Invoke(invocation.Arguments);
        else
#pragma warning disable MA0025
            throw new NotImplementedException();
#pragma warning restore MA0025
    }
}
