namespace Stl.Fusion.Blazor;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FusionComponentAttribute : Attribute
{
    public ParameterComparisonMode ParameterComparisonMode { get; }

    public FusionComponentAttribute(ParameterComparisonMode parameterComparisonMode)
        => ParameterComparisonMode = parameterComparisonMode;
}
