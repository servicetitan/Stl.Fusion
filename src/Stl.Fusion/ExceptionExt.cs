namespace Stl.Fusion;

public static class ExceptionExt
{
    public static Exception ToResultException(this Exception wrappedException) 
        => wrappedException is OperationCanceledException 
            ? wrappedException 
            : new(wrappedException.Message, wrappedException);

    public static Exception MaybeToResultException(this Exception sourceException, bool wrapToResultException)
        => wrapToResultException ? sourceException.ToResultException() : sourceException;
}
