using System;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class ReplicatorRequest : BridgeMessage
    {
        public Symbol ReplicatorId { get; set; }
    }
}
