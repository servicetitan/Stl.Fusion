using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor;

public class FusionComponentBase : ComponentBase
{
    private ComponentInfo? _componentInfo;

    protected ComponentInfo ComponentInfo => _componentInfo ??= ComponentInfo.Get(GetType());

    public override Task SetParametersAsync(ParameterView parameters)
        => ComponentInfo.ShouldSetParameters(this, parameters)
            ? base.SetParametersAsync(parameters)
            : Task.CompletedTask;
}
