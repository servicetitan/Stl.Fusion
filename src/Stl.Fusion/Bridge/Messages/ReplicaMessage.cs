using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class ReplicaMessage : PublicationMessage
    {
        public LTag ReplicaLTag { get; set; }
        public bool ReplicaIsConsistent { get; set; }
    }
}
