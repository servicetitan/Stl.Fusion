using System;
using Stl.Fusion.Publish.Messages;

namespace Stl.Fusion.Publish
{
    public readonly struct PublicationStateChanged
    {
        public IPublication Publication { get; }
        public Message? Message { get; }

        public PublicationStateChanged(IPublication publication, Message? message)
        {
            Publication = publication;
            Message = message;
        }

        public void Deconstruct(out IPublication publication, out Message? message)
        {
            publication = Publication;
            message = Message;
        }
    }
}
