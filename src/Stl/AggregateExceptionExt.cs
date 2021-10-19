namespace Stl;

/// <summary>
/// Extension methods for <see cref="AggregateException"/>.
/// </summary>
public static class AggregateExceptionExt
{
    public static Exception GetFirstInnerException(this AggregateException exception)
    {
        while (exception.InnerExceptions.Count > 0) {
            var e = exception.InnerExceptions[0];
            if (e is AggregateException ae)
                exception = ae;
            else
                return e;
        }
        return exception;
    }

}
