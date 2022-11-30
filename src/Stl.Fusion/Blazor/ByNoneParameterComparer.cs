namespace Stl.Fusion.Blazor;

public sealed class ByNoneParameterComparer : ParameterComparer
{
    public override bool AreEqual(object? oldValue, object? newValue)
        => true;
}
