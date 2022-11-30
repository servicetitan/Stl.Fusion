using Stl.Versioning;

namespace Stl.Fusion.Blazor;

public sealed class ByVersionParameterComparer<TVersion> : ParameterComparer
    where TVersion : notnull
{
    public override bool AreEqual(object? oldValue, object? newValue)
    {
        if (ReferenceEquals(oldValue, newValue))
            return true; // Might be the most frequent case
        if (oldValue == null)
            return newValue == null;
        if (newValue == null)
            return false;

        var oldVersion = ((IHasVersion<TVersion>) oldValue).Version;
        var newVersion = ((IHasVersion<TVersion>) newValue).Version;
        return EqualityComparer<TVersion>.Default.Equals(oldVersion, newVersion);
    }
}
