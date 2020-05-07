using System;

namespace Stl.Fusion.Publish.Messages
{
    [Serializable]
    public class ConsistentMessage<T> : PublicationMessage
    {
        public Result<T> Output { get; set; } 
        public int Tag { get; set; }
    }
}
