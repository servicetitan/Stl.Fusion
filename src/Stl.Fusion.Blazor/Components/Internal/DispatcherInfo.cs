using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor.Internal;

public static class DispatcherInfo
{
    // MAUI's Blazor doesn't flow ExecutionContext into InvokeAsync,
    // so we must detect & flow it in case it doesn't flow, otherwise
    // Computed.Current won't be available there.
    private static volatile int _executionContextFlowState;

    public static bool FlowsExecutionContext(ComponentBase anyComponent)
    {
        var flowState = (ExecutionContextFlowState)_executionContextFlowState;
        switch (flowState) {
        case ExecutionContextFlowState.Enabled:
            return true;
        case ExecutionContextFlowState.Disabled:
            return false;
        }

        var dispatcher = anyComponent.GetDispatcher();
        var dispatcherTypeName = dispatcher.GetType().Name;

        // NullRendered is used in WASM,
        // RendererSynchronizationContextDispatcher is used in Blazor Server
        var doesFlow = dispatcherTypeName is "NullDispatcher" or "RendererSynchronizationContextDispatcher";
        flowState = doesFlow ? ExecutionContextFlowState.Enabled : ExecutionContextFlowState.Disabled;
        Interlocked.Exchange(ref _executionContextFlowState, (int) flowState);
        return doesFlow;
    }

    // Nested types

    private enum ExecutionContextFlowState
    {
        Unknown = 0,
        Enabled,
        Disabled,
    }
}
