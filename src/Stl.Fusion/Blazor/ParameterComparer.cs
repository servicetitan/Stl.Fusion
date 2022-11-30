namespace Stl.Fusion.Blazor;

public abstract class ParameterComparer
{
    public abstract bool AreEqual(object? oldValue, object? newValue);
}
