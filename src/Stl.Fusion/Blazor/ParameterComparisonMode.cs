namespace Stl.Fusion.Blazor;

public enum ParameterComparisonMode
{
    Inherited = 0,
    Custom,
    Standard,
}

public static class ParameterComparisonModeExt
{
    public static ParameterComparisonMode? NullIfInherited(this ParameterComparisonMode mode)
        => mode == ParameterComparisonMode.Inherited ? null : mode;
}
