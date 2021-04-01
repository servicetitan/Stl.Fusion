using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Stl.Fusion.Blazor
{
    public static class ComponentEx
    {
        private static readonly Action<ComponentBase> CompiledStateHasChanged;
        private static readonly Func<ComponentBase, RenderHandle> CompiledGetRenderHandle;
        private static readonly Func<RenderHandle, object> CompiledGetOptionalComponentState;

        // InvokeAsync

        public static RenderHandle GetRenderHandle(this ComponentBase component)
            => CompiledGetRenderHandle.Invoke(component);

        public static bool IsDisposed(this RenderHandle renderHandle)
            => CompiledGetOptionalComponentState.Invoke(renderHandle) == null;

        /// <summary>
        /// Calls StateHasChanged() in the Blazor SynchronizationContext of the component
        /// Therefore it works even when called from another context such as threadpool thread
        /// </summary>
        public static Task StateHasChangedAsync(this ComponentBase component)
        {
#pragma warning disable 1998
            async Task Invoker()
#pragma warning restore 1998
            {
                // The component's renderer may already be disposed while the component is not yet disposed
                // Just calling StateHasChanged() will then cause an ObjectDisposedException.
                // Workaround: To figure out if the renderer is already disposed, we reflect into private
                // and protected members of the component
                if (component.GetRenderHandle().IsDisposed())
                    return;
                try {
                    CompiledStateHasChanged.Invoke(component);
                }
                catch (ObjectDisposedException) {
                    // Intended
                }
            }
            return component.GetRenderHandle().Dispatcher.InvokeAsync(Invoker);
        }

        static ComponentEx()
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
            CompiledGetOptionalComponentState = Expression.Lambda<Func<RenderHandle, object>>(
                Expression.Call(
                    Expression.Field(pRenderHandle, fRenderer),
                    mGetOptionalComponentState,
                    Expression.Field(pRenderHandle, fComponentId)),
                pRenderHandle).Compile();
        }
    }
}
