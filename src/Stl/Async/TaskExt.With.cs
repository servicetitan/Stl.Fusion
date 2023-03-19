namespace Stl.Async;

public static partial class TaskExt
{
    // WithErrorHandler

    public static async Task WithErrorHandler(this Task task, Action<Exception> errorHandler)
    {
        try {
            await task.ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            errorHandler(e);
            throw;
        }
    }

    public static async Task<T> WithErrorHandler<T>(this Task<T> task, Action<Exception> errorHandler)
    {
        try {
            return await task.ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            errorHandler(e);
            throw;
        }
    }

    // WithErrorLog

    public static Task WithErrorLog(this Task task, ILogger errorLog, string message)
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        => task.WithErrorHandler(e => errorLog.LogError(e, message));

    public static Task<T> WithErrorLog<T>(this Task<T> task, ILogger errorLog, string message)
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        => task.WithErrorHandler(e => errorLog.LogError(e, message));
}
