namespace Stl.Fusion.Blazor;

[AttributeUsage(
    AttributeTargets.Interface |
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Delegate |
    AttributeTargets.Property)]
public class ParameterComparerAttribute : Attribute
{
    public Type ComparerType { get; }

    public ParameterComparerAttribute(Type comparerType)
        => ComparerType = comparerType;
}
