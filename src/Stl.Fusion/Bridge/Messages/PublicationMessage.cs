using System;
using Newtonsoft.Json;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class PublicationMessage : Message
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long? MessageIndex { get; set; }
        public Symbol PublisherId { get; set; }
        public Symbol PublicationId { get; set; }
    }
}
