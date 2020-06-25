using System;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class PublicationMessage : PublisherMessage
    {
        public Symbol PublicationId { get; set; }
    }
}
