using System;

namespace Stl.Fusion.Publish.Messages
{
    [Serializable]
    public class InvalidatedMessage : PublicationMessage
    {
        public int Tag { get; set; }
    }
}
