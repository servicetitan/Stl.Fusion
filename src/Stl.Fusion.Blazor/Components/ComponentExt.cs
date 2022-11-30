using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Stl.Fusion.Blazor;

public static class ComponentExt
{
    private static readonly Action<ComponentBase> CompiledStateHasChanged;
    private static readonly Func<ComponentBase, RenderHandle> CompiledGetRenderHandle;
    private static readonly Func<RenderHandle, object?> CompiledGetOptionalComponentState;

    public static ComponentInfo GetComponentInfo(this IComponent component)
        => ComponentInfo.Get(component.GetType());
    public static Dispatcher GetDispatcher(this ComponentBase component)
        => component.GetRenderHandle().Dispatcher;

    public static bool IsDisposed(this ComponentBase component)
        => component.GetRenderHandle().IsDisposed();

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

    public static bool ShouldSetParameters(this IComponent component, ParameterView parameterView)
    {
        var componentInfo = component.GetComponentInfo();
        return componentInfo.ShouldSetParameters(component, parameterView);
    }

    // Internal and private methods

    internal static RenderHandle GetRenderHandle(this ComponentBase component)
        => CompiledGetRenderHandle(component);
    internal static bool IsDisposed(this RenderHandle renderHandle)
        => CompiledGetOptionalComponentState(renderHandle) == null;

    static ComponentExt()
    {
        var bfInstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
        var tComponentBase = typeof(ComponentBase);
        var tRenderHandle = typeof(RenderHandle);
        var tRenderer = typeof(Renderer);
        var mStateHasChanged = tComponentBase.GetMethod("StateHasChanged", bfInstanceNonPublic)!;
        var fRenderHandle = tComponentBase.GetField("_renderHandle", bfInstanceNonPublic)!;
        var fComponentId = tRenderHandle.GetField("_componentId", bfInstanceNonPublic)!;
        var fRenderer = tRenderHandle.GetField("_renderer", bfInstanceNonPublic)!;
        var mGetOptionalComponentState = tRenderer.GetMethod("GetOptionalComponentState", bfInstanceNonPublic)!;

        var pComponent = Expression.Parameter(typeof(ComponentBase), "component");
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
