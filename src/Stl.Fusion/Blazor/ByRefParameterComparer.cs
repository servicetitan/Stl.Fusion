namespace Stl.Fusion.Blazor;

public sealed class ByRefParameterComparer : ParameterComparer
{
    public override bool AreEqual(object? oldValue, object? newValue)
        => ReferenceEquals(oldValue, newValue);
}
