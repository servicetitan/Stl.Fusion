using System;
using Newtonsoft.Json;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class PublicationStateMessage : PublicationMessage
    {
        public LTag Version { get; set; }
        public bool IsConsistent { get; set; }
        public abstract Type GetResultType();
    }

    [Serializable]
    public class PublicationStateMessage<T> : PublicationStateMessage
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Result<T>? Output { get; set; }
        public override Type GetResultType() => typeof(T);
    }
}
