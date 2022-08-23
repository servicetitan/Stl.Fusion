using System.Globalization;
using Newtonsoft.Json.Linq;
using RestEase.Implementation;
using RestEase;

namespace Stl.Fusion.Client.RestEase.Internal;

/// <summary>
/// Serializes object parameters in a format compatible with asp.net core mvc model binding for complex types
/// https://docs.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-3.1#complex-types
/// </summary>
public class FusionRequestQueryParamSerializer : RequestQueryParamSerializer
{
        public override IEnumerable<KeyValuePair<string, string>> SerializeQueryParam<T>(string name, T value, RequestQueryParamSerializerInfo info)
           => Serialize(name, value, info);

        public override IEnumerable<KeyValuePair<string, string>> SerializeQueryCollectionParam<T>(string name, IEnumerable<T> values, RequestQueryParamSerializerInfo info)
            => Serialize(name, values, info);

        private IEnumerable<KeyValuePair<string, string>> Serialize<T>(string name, T value, RequestQueryParamSerializerInfo info)
        {
            if (value == null)
                yield break;

            foreach (var prop in GetPropertiesDeepRecursive(value, name))
            {
                if (prop.Value == null)
                {
                    yield return new KeyValuePair<string, string>(prop.Key, string.Empty);
                }
                else if (prop.Value is DateTime dt)
                {
                yield return new KeyValuePair<string, string>(prop.Key, dt.ToString(info.Format ?? "o"));
                }
                else
                {
                yield return new KeyValuePair<string, string>(prop.Key, prop.Value?.ToString() ?? "");
                }
            }
        }

        private Dictionary<string, object> GetPropertiesDeepRecursive(object obj, string name)
        {
            var dict = new Dictionary<string, object>(StringComparer.Ordinal);

            if (obj == null)
            {
                dict.Add(name, null);
                return dict;
            }

            if (obj.GetType().IsValueType || obj is string)
            {
                dict.Add(name, obj);
                return dict;
            }

            if (obj is IEnumerable collection)
            {
                int i = 0;
                foreach (var item in collection)
                {
                    dict = dict.Concat(GetPropertiesDeepRecursive(item, $"{name}[{i++}]")).ToDictionary(e => e.Key, e => e.Value);
                }
                return dict;
            }

            var properties = obj.GetType().GetProperties();
            var prefix = string.Empty;
            foreach (var prop in properties)
            {
                dict = dict
                    .Concat(GetPropertiesDeepRecursive(prop.GetValue(obj, null), $"{prefix}{prop.Name}"))
                    .ToDictionary(e => e.Key, e => e.Value);
            }
            return dict;
        }
    }
