using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Stl;
using Stl.ImmutableModel.Updating;

namespace Stl.ImmutableModel.Internal
{
    public class ChangeSetJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) 
            => objectType == typeof(ChangeSet);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var typeRef = (ChangeSet) value!;
            writer.WriteValue(typeRef.Changes.ToDictionary());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var value = (Dictionary<DomainKey, NodeChangeType>) reader.Value!;
            // ReSharper disable once HeapView.BoxingAllocation
            return new ChangeSet(value.ToImmutableDictionary());
        }
    }
}
