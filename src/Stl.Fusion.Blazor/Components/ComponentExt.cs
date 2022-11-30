using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Stl.Fusion.Blazor;

public static class ComponentExt
{
    private static readonly Action<ComponentBase> CompiledStateHasChanged;
    private static readonly Func<ComponentBase, bool> CompiledIsInitialized;
    private static readonly Func<ComponentBase, RenderHandle> CompiledGetRenderHandle;
    private static readonly Func<RenderHandle, object?> CompiledGetOptionalComponentState;

    public static ComponentInfo GetComponentInfo(this ComponentBase component)
        => ComponentInfo.Get(component.GetType());
    public static Dispatcher GetDispatcher(this ComponentBase component)
        => CompiledGetRenderHandle.Invoke(component).Dispatcher;

    public static bool IsInitialized(this ComponentBase component)
        => CompiledIsInitialized.Invoke(component);
    public static bool IsDisposed(this ComponentBase component)
        => CompiledGetRenderHandle.Invoke(component).IsDisposed();

    /// <summary>
    /// Calls <see cref="ComponentBase.StateHasChanged"/> in the Blazor synchronization context
    /// of the component, therefore it works even when called from another synchronization context
    /// (e.g. a thread-pool thread).
    /// </summary>
    public static Task StateHasChangedAsync(this ComponentBase component)
    {
        try {
            return component.GetDispatcher().InvokeAsync(Invoker);
        }
        catch (ObjectDisposedException) {
            return Task.CompletedTask;
        }

        void Invoker() {
            try {
                CompiledStateHasChanged(component);
            }
            catch (ObjectDisposedException) {
                // Intended
            }
        }
    }

    public static bool ShouldSetParameters(this ComponentBase component, ParameterView parameterView)
    {
        var componentInfo = component.GetComponentInfo();
        return componentInfo.ShouldSetParameters(component, parameterView) || !component.IsInitialized();
    }

    // Internal and private methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDisposed(this RenderHandle renderHandle)
        => CompiledGetOptionalComponentState.Invoke(renderHandle) == null;

    static ComponentExt()
    {
        var bfInstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        var tComponentBase = typeof(ComponentBase);
        var tRenderHandle = typeof(RenderHandle);
        var tRenderer = typeof(Renderer);
        var fInitialized = tComponentBase.GetField("_initialized", bfInstanceNonPublic)!;
        var fRenderHandle = tComponentBase.GetField("_renderHandle", bfInstanceNonPublic)!;
        var mStateHasChanged = tComponentBase.GetMethod("StateHasChanged", bfInstanceNonPublic)!;
        var fComponentId = tRenderHandle.GetField("_componentId", bfInstanceNonPublic)!;
        var fRenderer = tRenderHandle.GetField("_renderer", bfInstanceNonPublic)!;
        var mGetOptionalComponentState = tRenderer.GetMethod("GetOptionalComponentState", bfInstanceNonPublic)!;

        var pComponent = Expression.Parameter(typeof(ComponentBase), "component");
        CompiledIsInitialized = Expression.Lambda<Func<ComponentBase, bool>>(
            Expression.Field(pComponent, fInitialized), pComponent).Compile();

        var pRenderHandle = Expression.Parameter(typeof(RenderHandle), "renderHandle");
        CompiledStateHasChanged = Expression.Lambda<Action<ComponentBase>>(
            Expression.Call(pComponent, mStateHasChanged), pComponent).Compile();
        CompiledGetRenderHandle = Expression.Lambda<Func<ComponentBase, RenderHandle>>(
            Expression.Field(pComponent, fRenderHandle), pComponent).Compile();
        CompiledGetOptionalComponentState = Expression.Lambda<Func<RenderHandle, object?>>(
            Expression.Call(
                Expression.Field(pRenderHandle, fRenderer),
                mGetOptionalComponentState,
                Expression.Field(pRenderHandle, fComponentId)),
            pRenderHandle).Compile();
    }
}
