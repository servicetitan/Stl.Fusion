using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stl.Reflection;

namespace Stl.ImmutableModel.Internal
{
    public class NodeJsonConverter : JsonConverter 
    {
        public override bool CanConvert(Type objectType) 
            => typeof(INode).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var node = (INode) value!;
            var nodeInfo = value == null ? null : new NodeInfo() {
                Type = node.GetType().AssemblyQualifiedName,
                Key = node.Key.FormattedValue,
                Items = node.GetDefinition().GetAllItems(node).ToDictionary(p => p.Key.Value, p => p.Value),
            };
            serializer.Serialize(writer, nodeInfo);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var nodeInfo = serializer.Deserialize<NodeInfo>(reader);
            if (nodeInfo == null)
                return null;
            var children = new List<(Symbol Key, Option<object?> Value)>();
            foreach (var (k, v) in nodeInfo.Items!) {
                var key = new Symbol(k);
                var value = v;
                if (v is JObject jObject) {
                    var jToken = jObject[nameof(NodeInfo.Type)];
                    Type type;
                    if (jToken != null && jToken.Type == JTokenType.String) {
                        var typeName = jToken!.Value<string>();
                        type = new TypeRef(typeName).Resolve();
                    }
                    else {
                        type = objectType.GetProperty(k)!.PropertyType;
                    }
                    value = serializer.Deserialize(jObject.CreateReader(), type);
                }
                children.Add((key, Option.Some(value)));
            }
            var nodeCtor = (Func<INode>) objectType.GetConstructorDelegate();
            var node = nodeCtor.Invoke();
            var nodeTypeDef = node.GetDefinition();
            node.Key = Key.Parse(nodeInfo.Key!)!;
            foreach (var (localKey, value) in children)
                nodeTypeDef.SetItem(node, localKey, value);
            return node;
        }
    }
}
