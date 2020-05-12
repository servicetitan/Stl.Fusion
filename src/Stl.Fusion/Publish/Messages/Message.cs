using System;
using Newtonsoft.Json;
using Stl.Text;

namespace Stl.Fusion.Publish.Messages
{
    [Serializable]
    public abstract class Message
    {
        Symbol? Id { get; set; }

        public override string ToString()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            return $"{GetType().Name} {json}";
        }
    }
}
