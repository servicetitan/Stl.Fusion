using System;
using System.Text.Json.Serialization;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class PublicationStateChangedMessage : PublicationMessage
    {
        public LTag Version { get; set; }
        [JsonIgnore]
        public abstract bool IsConsistent { get; }
        public abstract Type GetResultType();
    }

    [Serializable]
    public class PublicationStateChangedMessage<T> : PublicationStateChangedMessage
    {
        public Result<T>? Output { get; set; }
        [JsonIgnore]
        public override bool IsConsistent => Output.HasValue;
        public override Type GetResultType() => typeof(T);
    }
}
