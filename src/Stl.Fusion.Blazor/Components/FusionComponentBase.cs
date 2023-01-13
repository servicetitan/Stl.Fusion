using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor;

public class FusionComponentBase : ComponentBase
{
    private ComponentInfo? _componentInfo;
    private bool _isInitialized;

    protected ComponentInfo ComponentInfo => _componentInfo ??= ComponentInfo.Get(GetType());

    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!_isInitialized) {
            _isInitialized = true;
            return base.SetParametersAsync(parameters);
        }
        return ComponentInfo.ShouldSetParameters(this, parameters)
            ? base.SetParametersAsync(parameters)
            : Task.CompletedTask;
    }
}
