using System;
using Stl.Text;

namespace Stl.Fusion.Publish.Messages
{
    [Serializable]
    public abstract class PublicationMessage : Message
    {
        public Symbol PublisherId { get; set; }
        public Symbol PublicationId { get; set; }
    }
}
