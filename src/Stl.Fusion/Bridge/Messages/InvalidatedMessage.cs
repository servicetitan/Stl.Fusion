using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public class InvalidatedMessage : PublicationMessage
    {
        public int Tag { get; set; }
    }
}
