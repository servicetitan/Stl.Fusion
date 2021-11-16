using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Stl.Internal;

namespace Stl.Fusion.Blazor;

public static class ComponentExt
{
    public record ComponentParameterInfo
    {
        public PropertyInfo Property { get; init; } = null!;
        public bool IsCascading { get; init; }
        public bool IsCapturingUnmatchedValues { get; init; }
        public string? CascadingParameterName { get; init; }
        public Func<IComponent, object> Getter { get; init; } = null!;
        public Action<IComponent, object> Setter { get; init; } = null!;
        public ParameterComparer Comparer { get; init; } = ParameterComparer.Default;
    }

    public record ComponentInfo
    {
        public Type Type { get; init; } = null!;
        public IReadOnlyDictionary<string, ComponentParameterInfo> Parameters { get; init; } =
            ImmutableDictionary<string, ComponentParameterInfo>.Empty;

        internal ComponentInfo(Type type)
        {
            if (!typeof(IComponent).IsAssignableFrom(type))
                throw new ArgumentOutOfRangeException(nameof(type));

            var bindingFlags = BindingFlags.FlattenHierarchy
                | BindingFlags.Instance
                | BindingFlags.Public | BindingFlags.NonPublic;
            var parameters = new Dictionary<string, ComponentParameterInfo>(StringComparer.Ordinal);
            foreach (var property in type.GetProperties(bindingFlags)) {
                var pa = property.GetCustomAttribute<ParameterAttribute>(true);
                CascadingParameterAttribute? cpa = null;
                if (pa == null) {
                    cpa = property.GetCustomAttribute<CascadingParameterAttribute>(true);
                    if (cpa == null)
                        continue;
                }
                var pca = property.GetCustomAttribute<ParameterComparerAttribute>(true);
                var comparerType = pca?.ComparerType;
                var comparer = comparerType != null
                    ? ParameterComparer.Get(comparerType)
                    : ParameterComparer.Default;
                var getter = type.GetGetter(property, true) as Func<IComponent, object>;
                if (getter == null)
                    throw Errors.InternalError($"Couldn't build getter delegate for {property}.");
                var setter = type.GetSetter(property, true) as Action<IComponent, object>;
                if (setter == null)
                    throw Errors.InternalError($"Couldn't build setter delegate for {property}.");
                var parameter = new ComponentParameterInfo() {
                    Property = property,
                    IsCascading = cpa != null,
                    IsCapturingUnmatchedValues = pa?.CaptureUnmatchedValues ?? false,
                    CascadingParameterName = cpa?.Name,
                    Getter = getter,
                    Setter = setter,
                    Comparer = comparer,
                };
                parameters.Add(parameter.Property.Name, parameter);
            }
            Type = type;
            Parameters = new ReadOnlyDictionary<string, ComponentParameterInfo>(parameters);
        }
    }

    private static readonly ConcurrentDictionary<Type, ComponentInfo> ComponentInfoCache = new();
    private static readonly Action<ComponentBase> CompiledStateHasChanged;
    private static readonly Func<ComponentBase, RenderHandle> CompiledGetRenderHandle;
    private static readonly Func<RenderHandle, object?> CompiledGetOptionalComponentState;

    public static ComponentInfo GetComponentInfo(Type componentType)
        => ComponentInfoCache.GetOrAdd(componentType, componentType1 => new ComponentInfo(componentType1));
    public static ComponentInfo GetComponentInfo(this IComponent component)
        => GetComponentInfo(component.GetType());
    public static Dispatcher GetDispatcher(this ComponentBase component)
        => component.GetRenderHandle().Dispatcher;

    public static bool IsDisposed(this ComponentBase component)
        => component.GetRenderHandle().IsDisposed();

    public static bool IsDisposedOrDisposing(this ComponentBase component, BlazorCircuitContext? blazorCircuitContext = null)
    {
        if (blazorCircuitContext?.IsDisposing ?? false)
            return true;
        if (component is StatefulComponentBase {
                UntypedState: IComputedState {
                    DisposeToken: { IsCancellationRequested: true }
                }
            })
            return true;
        return component.IsDisposed();
    }

    /// <summary>
    /// Calls <see cref="ComponentBase.StateHasChanged"/> in the Blazor synchronization context
    /// of the component, therefore it works even when called from another synchronization context
    /// (e.g. a thread-pool thread).
    /// </summary>
    public static Task StateHasChangedAsync(this ComponentBase component, BlazorCircuitContext? blazorCircuitContext = null)
    {
        if (component.IsDisposedOrDisposing())
            return Task.CompletedTask;
        return component.GetDispatcher().InvokeAsync(Invoker);

        void Invoker()
        {
            if (component.IsDisposedOrDisposing())
                return;
            try {
                CompiledStateHasChanged.Invoke(component);
            }
            catch (ObjectDisposedException) {
                // Intended: it might still happen
            }
        }
    }

    public static bool HasChangedParameters(this IComponent component, ParameterView parameterView)
    {
        var componentInfo = component.GetComponentInfo();
        var parameters = componentInfo.Parameters;
        foreach (var parameterValue in parameterView) {
            if (!parameters.TryGetValue(parameterValue.Name, out var parameterInfo))
                return true;
            var oldValue = parameterInfo.Getter.Invoke(component);
            if (!parameterInfo.Comparer.AreEqual(oldValue, parameterValue.Value))
                return true;
        }
        return false;
    }

    // Internal and private methods

    internal static RenderHandle GetRenderHandle(this ComponentBase component)
        => CompiledGetRenderHandle.Invoke(component);
    internal static bool IsDisposed(this RenderHandle renderHandle)
        => CompiledGetOptionalComponentState.Invoke(renderHandle) == null;

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
