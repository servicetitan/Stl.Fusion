namespace Stl.Interception;

public abstract class Interceptor
{
    public TResult Intercept<TResult>(Invocation invocation)
    {
        OnBefore();
        TResult result;
        Exception? error = null;
        try {
            var func = (Func<ArgumentList,TResult>)invocation.Delegate;
            result = func(invocation.Arguments);
            return result;
        }
        catch (Exception e) {
            error = e;
            throw;
        }
        finally {
            OnAfter(error);
        }
    }

    public void Intercept(Invocation invocation)
    {
        OnBefore();
        Exception? error = null;
        try {
            var action = (Action<ArgumentList>)invocation.Delegate;
            action(invocation.Arguments);
        }
        catch (Exception e) {
            error = e;
            throw;
        }
        finally {
            OnAfter(error);
        }
    }

    protected abstract void OnBefore();

    protected abstract void OnAfter(Exception? error);
}
