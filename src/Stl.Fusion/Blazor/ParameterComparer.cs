namespace Stl.Fusion.Blazor;

public abstract class ParameterComparer
{
    public static ParameterComparer Default { get; } = new DefaultParameterComparer();

    public abstract bool AreEqual(object? oldValue, object? newValue);
}
