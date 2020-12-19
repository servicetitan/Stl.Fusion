using System;
using Newtonsoft.Json;

namespace Stl.Fusion.Server.Messages
{
    [Serializable]
    public abstract class GatewayMessage
    {
        public override string ToString()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            return $"{GetType().Name} {json}";
        }
    }
}
