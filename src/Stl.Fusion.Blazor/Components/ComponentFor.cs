using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Stl.Fusion.Blazor;

/// <summary>
/// Renders a "dynamically bound" component.
/// </summary>
public class ComponentFor : ComponentBase
{
    /// <summary>
    /// The type of component to render.
    /// </summary>
    [Parameter]
    public Type? Type { get; set; } = null;

    /// <summary>
    /// The parameters of the component to set.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? Attributes { get; set; } = null;

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Type == null)
            return;
        var i = 0;
#pragma warning disable MA0123
        builder.OpenComponent(i++, Type);
        if (Attributes != null)
            foreach (var (key, value) in Attributes)
                builder.AddAttribute(i++, key, value);
        builder.CloseComponent();
#pragma warning restore MA0123
    }
}
