using System.Runtime.Serialization;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [DataContract]
    public abstract class PublicationReply : PublisherReply
    {
        [DataMember(Order = 2)]
        public Symbol PublicationId { get; set; }
    }
}
