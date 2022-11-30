namespace Stl.Fusion.Blazor;

public sealed class ByValueParameterComparer : ParameterComparer
{
    public override bool AreEqual(object? oldValue, object? newValue)
        => Equals(oldValue, newValue);
}
