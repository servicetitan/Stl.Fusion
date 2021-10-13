using System.Runtime.Serialization;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [DataContract]
    public abstract class ReplicaRequest : ReplicatorRequest
    {
        [DataMember(Order = 1)]
        public Symbol PublisherId { get; set; }
        [DataMember(Order = 2)]
        public Symbol PublicationId { get; set; }
    }
}
