namespace Stl.Fusion.Blazor;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class ParameterComparerAttribute : Attribute
{
    public Type ComparerType { get; }

    public ParameterComparerAttribute(Type comparerType)
        => ComparerType = comparerType;
}
