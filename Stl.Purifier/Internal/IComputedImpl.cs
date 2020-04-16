namespace Stl.Purifier.Internal
{
    public interface IComputedImpl : IComputed
    {
        void AddUsed(IComputedImpl used);
        void AddUsedBy(IComputedImpl usedBy); // Should be called only from AddUsedValue
        void RemoveUsedBy(IComputedImpl usedBy);
    }
}
