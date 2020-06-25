using System;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class ReplicaMessage : ReplicatorMessage
    {
        public Symbol PublisherId { get; set; }
        public Symbol PublicationId { get; set; }
    }
}
