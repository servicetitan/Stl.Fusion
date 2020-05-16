using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public class UpdatedMessage<T> : PublicationMessage
    {
        public Result<T> Output { get; set; } 
        public int Tag { get; set; }
    }
}
