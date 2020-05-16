using System;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public class SubscribeMessage : PublicationMessage
    {
        public bool IsUpdateRequested { get; set; } 
    }
}
