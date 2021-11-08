namespace Stl.Fusion.Blazor;

[AttributeUsage(AttributeTargets.Property)]
public class ParameterComparerAttribute : Attribute
{
    public Type ComparerType { get; }

    public ParameterComparerAttribute(Type comparerType)
        => ComparerType = comparerType;
}
