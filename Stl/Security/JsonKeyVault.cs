using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Stl.Async;

namespace Stl.Security
{
    public class JsonKeyVault : IKeyVault
    {
        public static SymbolListFormatter DottedPathParser = new SymbolListFormatter('.');

        protected JObject Secrets { get; }
        public bool IsReadOnly => true;

        public JsonKeyVault(JObject secrets) => Secrets = secrets;

        public JsonKeyVault(string fileName)
        {
            var json = File.ReadAllText(fileName);
            Secrets = JObject.Parse(json);
        } 

        public string? TryGetSecret(string key)
        {
            var path = DottedPathParser.Parse(key);
            var current = (JToken) Secrets;
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
            => ValueTaskEx.New(TryGetSecret(key));

        public void SetSecret(string key, string secret)
            => throw new NotSupportedException();
        public ValueTask SetSecretAsync(string key, string secret)
            => throw new NotSupportedException();
    }
}
