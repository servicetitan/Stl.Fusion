namespace Stl.Fusion.Internal;

public interface IComputedImpl : IComputed
{
    IComputedImpl[] Used { get; }
    (ComputedInput Input, LTag Version)[] UsedBy { get; }

    void AddUsed(IComputedImpl used);
    void AddUsedBy(IComputedImpl usedBy); // Should be called only from AddUsedValue
    void RemoveUsedBy(IComputedImpl usedBy);
    (int OldCount, int NewCount) PruneUsedBy();

    void RenewTimeouts();
    void CancelTimeouts();

    bool IsTransientError(Exception error);
}
