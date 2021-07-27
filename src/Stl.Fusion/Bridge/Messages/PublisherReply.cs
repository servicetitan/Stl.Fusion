using System;
using Newtonsoft.Json;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class PublisherReply : BridgeMessage
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long? MessageIndex { get; set; }
        public Symbol PublisherId { get; set; }
    }
}
