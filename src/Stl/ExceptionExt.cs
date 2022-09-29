namespace Stl;

/// <summary>
/// Extension methods for <see cref="Exception"/> type and its descendants.
/// </summary>
public static class ExceptionExt
{
    public static ICollection<Exception> Flatten(this Exception? exception)
    {
        if (exception == null)
            return Array.Empty<Exception>();

        var result = new List<Exception>();
        Traverse(result, exception);
        return result;

        void Traverse(List<Exception> list, Exception ex)
        {
            if (ex is AggregateException ae) {
                foreach (var e in ae.InnerExceptions) {
                    if (e != null!)
                        Traverse(list, e);
                }
            }
            if (ex.InnerException is { } ie) {
                Traverse(list, ie);
            }
            list.Add(ex);
        }
    }

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
