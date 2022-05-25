using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Stl.Extensibility;
using Stl.Fusion.Blazor.Internal;

namespace Stl.Fusion.Blazor;

/// <summary>
/// Finds and renders a component matching <see cref="Source"/> type
/// relying on <see cref="MatchingTypeFinder"/> / <see cref="MatchForAttribute"/>.
/// </summary>
public class MatchingComponentFor : ComponentBase
{
    /// <summary>
    /// Matching type finder (auto-injected).
    /// </summary>
    [Inject] protected IMatchingTypeFinder MatchingTypeFinder { get; init; } = null!;

    /// <summary>
    /// Scope to use with <see cref="MatchingTypeFinder"/>.
    /// </summary>
    [Parameter]
    public string Scope { get; set; } = "";

    /// <summary>
    /// Source entity, which type determines the right component to pick.
    /// </summary>
    [Parameter]
    public object? Source { get; set; } = default!;

    /// <summary>
    /// Name of the component's parameter to set <see cref="Source"/> value to.
    /// Null or empty name indicates it shouldn't be set.
    /// </summary>
    [Parameter]
    public string SourceParameterName { get; set; } = "Source";

    /// <summary>
    /// The content to render when the source is null.
    /// </summary>
    [Parameter]
    public RenderFragment? WhenNull { get; set; }

    /// <summary>
    /// The content to render when no match is found.
    /// </summary>
    [Parameter]
    public RenderFragment<object>? WhenNoMatchFound { get; set; }

    /// <summary>
    /// The parameters of the component to set.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? Attributes { get; set; } = null;

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Source == null) {
            WhenNull?.Invoke(builder);
            return;
        }

        var componentType = MatchingTypeFinder.TryFind(Source.GetType(), Scope);
        if (componentType == null) {
            if (WhenNoMatchFound == null)
                throw Errors.NoMatchingComponentFound(Source.GetType(), Scope);
            WhenNoMatchFound(Source)(builder);
            return;
        }

        var i = 0;
        builder.OpenComponent(i++, componentType);
        if (!string.IsNullOrEmpty(SourceParameterName))
            builder.AddAttribute(i++, SourceParameterName, Source);
        if (Attributes != null)
            foreach (var (key, value) in Attributes)
                builder.AddAttribute(i++, key, value);
        builder.CloseComponent();
    }
}
