namespace Stl.Async;

public readonly struct BatchItem<TIn, TOut>
{
    public TIn Input { get; }
    public CancellationToken CancellationToken { get; }
    public Task<TOut> OutputTask { get; }

    public BatchItem(TIn input, CancellationToken cancellationToken, Task<TOut> outputTask)
    {
        Input = input;
        CancellationToken = cancellationToken;
        OutputTask = outputTask;
    }

    public void Deconstruct(out TIn input, out CancellationToken cancellationToken, out Task<TOut> outputTask)
    {
        input = Input;
        cancellationToken = CancellationToken;
        outputTask = OutputTask;
    }

    public override string ToString()
        => $"{GetType().GetName()}({Input}, {CancellationToken}, {OutputTask})";

    public bool TryCancel(CancellationToken cancellationToken = default)
    {
        if (CancellationToken.IsCancellationRequested)
            TaskSource.For(OutputTask).TrySetCanceled(CancellationToken);
        else if (cancellationToken.IsCancellationRequested)
            TaskSource.For(OutputTask).TrySetCanceled(cancellationToken);
        return OutputTask.IsCanceled;
    }

    public void SetResult(Result<TOut> result, CancellationToken candidateToken)
        => TaskSource.For(OutputTask).TrySetFromResult(result, candidateToken);
}
