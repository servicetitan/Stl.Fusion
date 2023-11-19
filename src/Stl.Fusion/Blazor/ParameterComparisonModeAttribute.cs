namespace Stl.Fusion.Blazor;

#pragma warning disable CA1813 // Consider making sealed

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FusionComponentAttribute(ParameterComparisonMode parameterComparisonMode) : Attribute
{
    public ParameterComparisonMode ParameterComparisonMode { get; } = parameterComparisonMode;
}
