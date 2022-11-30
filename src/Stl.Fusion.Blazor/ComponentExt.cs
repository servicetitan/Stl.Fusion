using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

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
        public Type Type { get; init; }
        public bool HasCustomParameterComparers { get; init; }
        public IReadOnlyDictionary<string, ComponentParameterInfo> Parameters { get; init; }

        internal ComponentInfo(Type type)
        {
            if (!typeof(IComponent).IsAssignableFrom(type))
                throw new ArgumentOutOfRangeException(nameof(type));

            var bindingFlags = BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public;
            var parameters = new Dictionary<string, ComponentParameterInfo>(StringComparer.Ordinal);
            var hasCustomParameterComparers = false;
            foreach (var property in type.GetProperties(bindingFlags)) {
                var pa = property.GetCustomAttribute<ParameterAttribute>(true);
                CascadingParameterAttribute? cpa = null;
                if (pa == null) {
                    cpa = property.GetCustomAttribute<CascadingParameterAttribute>(true);
                    if (cpa == null)
                        continue; // Not a parameter
                }

                var pca = property.GetCustomAttribute<ParameterComparerAttribute>(true);
                var comparerType = pca?.ComparerType;
                var comparer = comparerType != null
                    ? ParameterComparer.Get(comparerType)
                    : ParameterComparer.Default;

                hasCustomParameterComparers |= comparer is not DefaultParameterComparer;
                var parameter = new ComponentParameterInfo() {
                    Property = property,
                    IsCascading = cpa != null,
                    IsCapturingUnmatchedValues = pa?.CaptureUnmatchedValues ?? false,
                    CascadingParameterName = cpa?.Name,
                    Getter = property.GetGetter<IComponent, object>(true),
                    Setter = property.GetSetter<IComponent, object>(true),
                    Comparer = comparer,
                };
                parameters.Add(parameter.Property.Name, parameter);
            }
            Type = type;
            Parameters = new ReadOnlyDictionary<string, ComponentParameterInfo>(parameters);
            HasCustomParameterComparers = hasCustomParameterComparers;
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

    public static bool HasChangedParameters(this IComponent component, ParameterView parameterView)
    {
        var componentInfo = component.GetComponentInfo();
        if (!componentInfo.HasCustomParameterComparers)
            return true; // No custom comparers -> trigger default flow

        var parameters = componentInfo.Parameters;
        foreach (var parameterValue in parameterView) {
            if (!parameters.TryGetValue(parameterValue.Name, out var parameterInfo))
                return true; // Unknown parameter -> trigger default flow

            var oldValue = parameterInfo.Getter(component);
            if (!parameterInfo.Comparer.AreEqual(oldValue, parameterValue.Value))
                return true; // Comparer says values aren't equal -> trigger default flow
        }
        return false; // All parameter values are equal
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
