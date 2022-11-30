namespace Stl.Fusion.Blazor;

public sealed class ByReferenceParameterComparer : ParameterComparer
{
    public override bool AreEqual(object? oldValue, object? newValue)
        => ReferenceEquals(oldValue, newValue);
}
