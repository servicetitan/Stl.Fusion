namespace Stl.Async;

public readonly record struct BatchItem<TIn, TOut>(
    TIn Input,
    CancellationToken CancellationToken,
    TaskCompletionSource<TOut> OutputSource)
{
    public Task<TOut> OutputTask => OutputSource.Task;

    public override string ToString()
        => $"{GetType().GetName()}({Input}, {CancellationToken}, {OutputTask})";

    public bool TryCancel()
    {
        if (!CancellationToken.IsCancellationRequested)
            return false;
        OutputSource.TrySetCanceled(CancellationToken);
        return true;
    }

    public void Cancel(CancellationToken cancellationToken)
        => OutputSource.TrySetCanceled(CancellationToken.IsCancellationRequested ? CancellationToken : cancellationToken);

    public void SetResult(Result<TOut> result)
        => OutputSource.TrySetFromResult(result);

    public void SetResult(Result<TOut> result, CancellationToken cancellationToken)
        => OutputSource.TrySetFromResult(result, cancellationToken);
}
