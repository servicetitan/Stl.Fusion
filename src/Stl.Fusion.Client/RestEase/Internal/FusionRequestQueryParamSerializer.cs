using System.Globalization;
using RestEase;

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

        private IEnumerable<KeyValuePair<string, string?>> Serialize<T>(string name, T value, RequestQueryParamSerializerInfo info)
        {
            if (value == null)
                yield break;

            foreach (var p in GetPropertyMap(value, name)) {
                var sValue = p.Value switch {
                    null => "",
                    DateTime dateTime => dateTime.ToString(info.Format ?? "o", CultureInfo.InvariantCulture),
                    _ => p.Value?.ToString() ?? "",
                };
                yield return new KeyValuePair<string, string?>(p.Key, sValue);
            }
        }

        private Dictionary<string, object?> GetPropertyMap(
            object? source, string name, 
            Dictionary<string, object?>? map = null)
        {
            map ??= new Dictionary<string, object?>(StringComparer.Ordinal);

            if (ReferenceEquals(source, null))
                map.Add(name, null);
            else if (source.GetType().IsValueType || source is string)
                map.Add(name, source);
            else if (source is IEnumerable sequence) {
                var i = 0;
                foreach (var item in sequence)
                    GetPropertyMap(item, $"{name}[{i++}]", map);
            }
            else {
                var prefix = name.IsNullOrEmpty() ? "" : $"{name}.";
                foreach (var p in source.GetType().GetProperties())
                    GetPropertyMap(p.GetValue(source, null), $"{prefix}{p.Name}");
            }
            return map;
        }
    }
