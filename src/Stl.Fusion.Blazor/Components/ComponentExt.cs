#if !NET8_0_OR_GREATER
using System.Reflection.Emit;
#else
using Microsoft.AspNetCore.Components.Rendering;
#endif
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Stl.Fusion.Blazor;

public static class ComponentExt
{
#if USE_UNSAFE_ACCESSORS && NET8_0_OR_GREATER
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_renderHandle")]
    private static extern ref RenderHandle RenderHandleGetter(ComponentBase @this);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_initialized")]
    private static extern ref bool IsInitializedGetter(ComponentBase @this);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_renderer")]
    private static extern ref Renderer? RendererGetter(RenderHandle @this);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_componentId")]
    private static extern ref int ComponentIdGetter(RenderHandle @this);
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "StateHasChanged")]
    private static extern void StateHasChangedInvoker(ComponentBase @this);
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetOptionalComponentState")]
    private static extern ComponentState? GetOptionalComponentStateGetter(Renderer @this, int componentId);

    private static ComponentState? GetOptionalComponentStateGetter(RenderHandle renderHandle)
    {
        var renderer = RendererGetter(renderHandle);
        if (renderer == null)
            return null;

        var componentId = ComponentIdGetter(renderHandle);
        return GetOptionalComponentStateGetter(renderer, componentId);
    }
#else
    private static readonly Func<ComponentBase, RenderHandle> RenderHandleGetter;
    private static readonly Func<ComponentBase, bool> IsInitializedGetter;
    private static readonly Action<ComponentBase> StateHasChangedInvoker;
    private static readonly Func<RenderHandle, object?> GetOptionalComponentStateGetter;
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInfo GetComponentInfo(this ComponentBase component)
        => ComponentInfo.Get(component.GetType());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dispatcher GetDispatcher(this ComponentBase component)
        => RenderHandleGetter(component).Dispatcher;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInitialized(this ComponentBase component)
        => IsInitializedGetter(component);

    public static bool IsDisposed(this ComponentBase component)
    {
        var renderHandle = RenderHandleGetter(component);
        return GetOptionalComponentStateGetter(renderHandle) == null;
    }

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
                StateHasChangedInvoker(component);
            else if (component is FusionComponentBase fc)
                _ = dispatcher.InvokeAsync(fc.StateHasChangedInvoker);
            else
                _ = dispatcher.InvokeAsync(() => StateHasChangedInvoker(component));
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

#if !NET8_0_OR_GREATER
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
#endif
}
