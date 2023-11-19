using System.Reflection.Emit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Stl.OS;

namespace Stl.Fusion.Blazor;

public static class ComponentExt
{
    internal static readonly Action<ComponentBase> StateHasChangedInvoker;
    private static readonly Func<ComponentBase, bool> IsInitializedGetter;
    private static readonly Func<ComponentBase, RenderHandle> RenderHandleGetter;
    private static readonly Func<RenderHandle, object?> GetOptionalComponentStateGetter;

    public static ComponentInfo GetComponentInfo(this ComponentBase component)
        => ComponentInfo.Get(component.GetType());
    public static Dispatcher GetDispatcher(this ComponentBase component)
        => RenderHandleGetter.Invoke(component).Dispatcher;

    public static bool IsInitialized(this ComponentBase component)
        => IsInitializedGetter.Invoke(component);
    public static bool IsDisposed(this ComponentBase component)
        => RenderHandleGetter.Invoke(component).IsDisposed();

    /// <summary>
    /// Calls <see cref="ComponentBase.StateHasChanged"/> in the Blazor synchronization context
    /// of the component, therefore it works even when called from another synchronization context
    /// (e.g. a thread-pool thread).
    /// </summary>
    public static void NotifyStateHasChanged(this ComponentBase component)
    {
        try {
            var dispatcher = component.GetDispatcher();
#pragma warning disable IL2026
            if (dispatcher.IsNullDispatcher())
#pragma warning restore IL2026
                StateHasChangedInvoker.Invoke(component);
            else if (component is FusionComponentBase fc)
                _ = dispatcher.InvokeAsync(fc.StateHasChangedInvoker);
            else
                _ = dispatcher.InvokeAsync(() => StateHasChangedInvoker.Invoke(component));
        }
        catch (ObjectDisposedException) {
            // Intended
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
        => GetOptionalComponentStateGetter.Invoke(renderHandle) == null;

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

        IsInitializedGetter = fInitialized.GetGetter<ComponentBase, bool>();
        RenderHandleGetter = fRenderHandle.GetGetter<ComponentBase, RenderHandle>();
        StateHasChangedInvoker = (Action<ComponentBase>)mStateHasChanged.CreateDelegate(typeof(Action<ComponentBase>));

        var m = new DynamicMethod("_GetOptionalComponentState", typeof(object), new [] { typeof(RenderHandle) }, true);
        var il = m.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, fRenderer);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, fComponentId);
        il.Emit(OpCodes.Callvirt, mGetOptionalComponentState);
        il.Emit(OpCodes.Ret);
        GetOptionalComponentStateGetter = (Func<RenderHandle, object?>)m.CreateDelegate(typeof(Func<RenderHandle, object?>));
    }
}
