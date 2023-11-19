namespace Stl.Fusion.Blazor;

#pragma warning disable CA1813 // Consider making sealed

[AttributeUsage(
    AttributeTargets.Interface |
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Delegate |
    AttributeTargets.Property)]
public class ParameterComparerAttribute : Attribute
{
    public Type ComparerType { get; }

    public ParameterComparerAttribute(Type comparerType)
        => ComparerType = comparerType;
}
