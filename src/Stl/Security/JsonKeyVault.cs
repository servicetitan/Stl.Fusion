using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stl.Async;
using Stl.Text;

namespace Stl.Security
{
    public class JsonKeyVault : IKeyVault
    {
        public static SymbolListFormatter ColumnPathParser = new SymbolListFormatter(':');

        protected JObject Vault { get; }

        public JsonKeyVault(JObject vault) => Vault = vault;

        public JsonKeyVault(string fileName)
        {
            var json = File.ReadAllText(fileName);
            Vault = JObject.Parse(json);
        }

        public string? TryGetSecret(string key)
        {
            var path = ColumnPathParser.Parse(key);
            var current = (JToken) Vault;
            foreach (var propertyName in path.GetSegments()) {
                if (!(current is JObject jObject))
                    return null;
                if (!jObject.TryGetValue(propertyName.Value, StringComparison.Ordinal, out var value))
                    return null;
                current = value;
            }

            if (!(current is JValue jValue))
                return null;
            if (jValue.Type != JTokenType.String)
                return null;
            return (string?) jValue.Value;
        }

        public ValueTask<string?> TryGetSecretAsync(string key)
            => ValueTaskEx.FromResult(TryGetSecret(key));
    }
}
