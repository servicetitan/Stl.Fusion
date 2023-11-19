using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Stl.Fusion.Blazor;

public sealed class ComponentInfo
{
    private static readonly ConcurrentDictionary<Type, ComponentInfo> ComponentInfoCache = new();

    public Type Type { get; }
    public bool HasCustomParameterComparers { get; }
    public ParameterComparisonMode ParameterComparisonMode { get; }
    public IReadOnlyDictionary<string, ComponentParameterInfo> Parameters { get; }

    public static ComponentInfo Get(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
#pragma warning disable IL2067
        => ComponentInfoCache.GetOrAdd(componentType, static t => new ComponentInfo(t));
#pragma warning restore IL2067

    private ComponentInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        if (!typeof(IComponent).IsAssignableFrom(type))
            throw new ArgumentOutOfRangeException(nameof(type));

        ComponentInfo? parentComponentInfo = null;
        if (typeof(IComponent).IsAssignableFrom(type.BaseType))
            parentComponentInfo = Get(type.BaseType!);

        var parameterComparerProvider = ParameterComparerProvider.Instance;
        var parameters = new Dictionary<string, ComponentParameterInfo>(StringComparer.Ordinal);
        if (parentComponentInfo != null)
            parameters.AddRange(parentComponentInfo.Parameters);

        var hasCustomParameterComparers = parentComponentInfo?.HasCustomParameterComparers ?? false;
        var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        foreach (var property in type.GetProperties(bindingFlags)) {
            var pa = property.GetCustomAttribute<ParameterAttribute>(true);
            CascadingParameterAttribute? cpa = null;
            if (pa == null) {
                cpa = property.GetCustomAttribute<CascadingParameterAttribute>(true);
                if (cpa == null)
                    continue; // Not a parameter
            }

#pragma warning disable IL2026
            var comparer = parameterComparerProvider.Get(property);
#pragma warning restore IL2026
            hasCustomParameterComparers |= comparer is not DefaultParameterComparer;
            var parameter = new ComponentParameterInfo() {
                Property = property,
                IsCascading = cpa != null,
                IsCapturingUnmatchedValues = pa?.CaptureUnmatchedValues ?? false,
                CascadingParameterName = cpa?.Name,
                Comparer = comparer,
            };
            parameters.Add(parameter.Property.Name, parameter);
        }
        Type = type;
        Parameters = new ReadOnlyDictionary<string, ComponentParameterInfo>(parameters);
        HasCustomParameterComparers = hasCustomParameterComparers;

        var fca = type.GetCustomAttribute<FusionComponentAttribute>(false);
        ParameterComparisonMode = fca?.ParameterComparisonMode.NullIfInherited()
            ?? parentComponentInfo?.ParameterComparisonMode
            ?? FusionComponentBase.DefaultParameterComparisonMode;
    }

    public bool ShouldSetParameters(ComponentBase component, ParameterView parameterView)
    {
        if (!HasCustomParameterComparers || ParameterComparisonMode == ParameterComparisonMode.Standard)
            return true; // No custom comparers -> trigger the default flow

        var parameters = Parameters;
        foreach (var parameterValue in parameterView) {
            if (!parameters.TryGetValue(parameterValue.Name, out var parameterInfo))
                return true; // Unknown parameter -> trigger default flow

            var oldValue = parameterInfo.Getter.Invoke(component);
            if (!parameterInfo.Comparer.AreEqual(oldValue, parameterValue.Value))
                return true; // Comparer says values aren't equal -> trigger default flow
        }

        return false; // All parameter values are equal
    }
}
