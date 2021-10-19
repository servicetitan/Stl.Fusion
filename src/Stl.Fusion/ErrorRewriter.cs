namespace Stl.Fusion;

public interface IErrorRewriter
{
    Exception Rewrite(object requester, Exception error, bool rewriteOperationCancelledException = false);
}

public class ErrorRewriter : IErrorRewriter
{
    public Exception Rewrite(object requester, Exception error, bool rewriteOperationCancelledException)
        => error switch {
            OperationCanceledException _ => rewriteOperationCancelledException ? new ServiceException(error) : error,
            ServiceException _ => error,
            _ => new ServiceException(error)
        };
}
