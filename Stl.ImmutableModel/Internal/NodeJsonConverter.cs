using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                Key = node.LocalKey.Value,
                Items = node.DualGetItems().ToDictionary(p => p.Key.Value, p => p.Value),
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
                    var typeName = jObject[nameof(NodeInfo.Type)]!.Value<string>();
                    var type = Type.GetType(typeName, true, false);
                    value = serializer.Deserialize(jObject.CreateReader(), type);
                }
                children.Add((key, Option.Some(value)));
            }
            // ReSharper disable once HeapView.BoxingAllocation
            var node = (INode) Activator.CreateInstance(objectType, new LocalKey(nodeInfo.Key!))!;
            return node.DualWith(children);
        }
    }
}
