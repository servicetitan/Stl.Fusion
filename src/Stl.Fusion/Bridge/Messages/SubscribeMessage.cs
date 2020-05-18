using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public class SubscribeMessage : ReplicaMessage
    {
        public bool IsUpdateRequested { get; set; }
    }
}
