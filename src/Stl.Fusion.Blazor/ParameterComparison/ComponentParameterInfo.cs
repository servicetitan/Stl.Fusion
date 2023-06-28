using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor;

public sealed class ComponentParameterInfo
{
    private Func<IComponent, object>? _getter;
    private Action<IComponent, object>? _setter;

    public PropertyInfo Property { get; init; } = null!;
    public bool IsCascading { get; init; }
    public bool IsCapturingUnmatchedValues { get; init; }
    public string? CascadingParameterName { get; init; }
    public Func<IComponent, object> Getter => _getter ??= Property.GetGetter<IComponent, object>(true);
    public Action<IComponent, object> Setter => _setter ??= Property.GetSetter<IComponent, object>(true);
    public ParameterComparer Comparer { get; init; } = null!;
}
