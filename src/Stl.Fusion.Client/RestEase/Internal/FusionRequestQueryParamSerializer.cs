using System.Globalization;
using RestEase;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;

namespace Stl.Fusion.Client.RestEase.Internal;

/// <summary>
/// Serializes object parameters in a format compatible with ASP.NET Core MVC
/// model binding for complex types.
/// https://docs.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-3.1#complex-types
/// </summary>
public class FusionRequestQueryParamSerializer : RequestQueryParamSerializer
{
    public override IEnumerable<KeyValuePair<string, string?>> SerializeQueryParam<T>(
        string name, T value, RequestQueryParamSerializerInfo info)
        => Serialize(name, value, info);

    public override IEnumerable<KeyValuePair<string, string?>> SerializeQueryCollectionParam<T>(
        string name, IEnumerable<T> values, RequestQueryParamSerializerInfo info)
        => Serialize(name, values, info);

    protected IEnumerable<KeyValuePair<string, string?>> Serialize<T>(
        string name, T value, 
        RequestQueryParamSerializerInfo info)
    {
        if (ReferenceEquals(value, null))
            yield break;
        var sValue = SerializeSimpleType(value, info);
        if (sValue != null) {
            yield return new KeyValuePair<string, string?>(name, sValue);
            yield break;
        }
        foreach (var (key, sValue1) in SerializeComplexType(name, value, info))
            yield return new KeyValuePair<string, string?>(key, sValue1);
    }

    protected virtual string? SerializeSimpleType(
        object source,
        RequestQueryParamSerializerInfo info)
    {
        if (source is string or Session or PageRef)
            return source.ToString() ?? "";
        if (source.GetType().IsValueType)
            return source is DateTime dateTime
                ? dateTime.ToString(info.Format ?? "o", CultureInfo.InvariantCulture)
                : source.ToString() ?? "";
        return null;
    }

    protected virtual Dictionary<string, string> SerializeComplexType(
        string name, object? value,
        RequestQueryParamSerializerInfo info,
        Dictionary<string, string>? map = null)
    {
        map ??= new Dictionary<string, string>(StringComparer.Ordinal);

        // Null
        if (ReferenceEquals(value, null)) {
            map.Add(name, "");
            return map;
        }

        // Simple type
        var serialized = SerializeSimpleType(value, info);
        if (serialized != null) {
            map.Add(name, serialized);
            return map;
        }

        // Collection
        if (value is IEnumerable collection) {
            var index = 0;
            foreach (var item in collection)
                SerializeComplexType($"{name}[{index++}]", item, info, map);
            return map;
        }

        // Complex type
        var prefix = name.IsNullOrEmpty() ? "" : $"{name}.";
        foreach (var property in value.GetType().GetProperties()) {
            var pValue = property.GetValue(value, null);
            SerializeComplexType($"{prefix}{property.Name}", pValue, info, map);
        }
        return map;
    }
}
