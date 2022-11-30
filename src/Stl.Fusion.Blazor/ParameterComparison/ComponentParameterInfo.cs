using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor;

public sealed class ComponentParameterInfo
{
    public PropertyInfo Property { get; init; } = null!;
    public bool IsCascading { get; init; }
    public bool IsCapturingUnmatchedValues { get; init; }
    public string? CascadingParameterName { get; init; }
    public Func<IComponent, object> Getter { get; init; } = null!;
    public Action<IComponent, object> Setter { get; init; } = null!;
    public ParameterComparer Comparer { get; init; } = null!;
}
