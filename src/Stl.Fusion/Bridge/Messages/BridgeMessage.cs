using System.Runtime.Serialization;
using Stl.Serialization;

namespace Stl.Fusion.Bridge.Messages
{
    [DataContract]
    public abstract class BridgeMessage
    {
        public override string ToString()
        {
            var json = SystemJsonSerializer.Readable.Write(this, GetType());
            return $"{GetType().Name} {json}";
        }
    }
}
