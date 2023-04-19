namespace Stl.Async;

public readonly record struct BatchItem<TIn, TOut>(
    TIn Input,
    CancellationToken CancellationToken,
    TaskCompletionSource<TOut> OutputSource)
{
    public Task<TOut> OutputTask => OutputSource.Task;

    public override string ToString()
        => $"{GetType().GetName()}({Input}, {CancellationToken}, {OutputTask})";

    public bool TryCancel(CancellationToken cancellationToken = default)
    {
        if (CancellationToken.IsCancellationRequested)
            OutputSource.TrySetCanceled(CancellationToken);
        else if (cancellationToken.IsCancellationRequested)
            OutputSource.TrySetCanceled(cancellationToken);
        return OutputTask.IsCanceled;
    }

    public void SetResult(Result<TOut> result, CancellationToken candidateToken)
        => OutputSource.TrySetFromResult(result, candidateToken);
}
