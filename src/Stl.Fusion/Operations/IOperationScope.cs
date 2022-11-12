namespace Stl.Fusion.Operations;

public interface IOperationScope : IAsyncDisposable, IRequirementTarget
{
    IOperation Operation { get; }
    CommandContext CommandContext { get; }
    bool IsUsed { get; }
    bool IsClosed { get; }
    bool? IsConfirmed { get; }

    Task Commit(CancellationToken cancellationToken = default);
    Task Rollback();
}
