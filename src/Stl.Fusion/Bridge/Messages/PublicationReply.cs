using System;
using Stl.Text;

namespace Stl.Fusion.Bridge.Messages
{
    [Serializable]
    public abstract class PublicationReply : PublisherReply
    {
        public Symbol PublicationId { get; set; }
    }
}
