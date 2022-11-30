using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor;

public class FusionComponentBase : ComponentBase
{
    public override Task SetParametersAsync(ParameterView parameters)
        => this.HasChangedParameters(parameters) ? base.SetParametersAsync(parameters) : Task.CompletedTask;
}
