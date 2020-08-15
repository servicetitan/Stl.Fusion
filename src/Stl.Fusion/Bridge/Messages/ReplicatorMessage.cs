using System;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class ReplicatorMessage : Message
    {
        public Symbol ReplicatorId { get; set; }
    }
}
