using System;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class ReplicaRequest : ReplicatorRequest
    {
        public Symbol PublisherId { get; set; }
        public Symbol PublicationId { get; set; }
    }
}
