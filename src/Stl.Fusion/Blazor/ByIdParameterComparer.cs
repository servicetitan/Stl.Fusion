namespace Stl.Fusion.Blazor;

public sealed class ByIdParameterComparer<TId> : ParameterComparer
{
    public override bool AreEqual(object? oldValue, object? newValue)
    {
        if (ReferenceEquals(oldValue, newValue))
            return true; // Might be the most frequent case
        if (oldValue == null)
            return newValue == null;
        if (newValue == null)
            return false;

        var oldId = ((IHasId<TId>) oldValue).Id;
        var newId = ((IHasId<TId>) newValue).Id;
        return EqualityComparer<TId>.Default.Equals(oldId, newId);
    }
}
