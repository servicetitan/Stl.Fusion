using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public class SubscribeRequest : ReplicaRequest
    {
        public LTag Version { get; set; }
        public bool IsConsistent { get; set; }
        public bool IsUpdateRequested { get; set; }
    }
}
