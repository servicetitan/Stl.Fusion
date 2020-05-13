using System;

namespace Stl.Fusion.Messages
{
    [Serializable]
    public class InvalidatedMessage : PublicationMessage
    {
        public int Tag { get; set; }
    }
}
