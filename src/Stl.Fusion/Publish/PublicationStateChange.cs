using Stl.Fusion.Publish.Messages;

namespace Stl.Fusion.Publish
{
    public readonly struct PublicationStateChange
    {
        public IPublication Publication { get; }
        public PublicationState PreviousState { get; }
        public Message? Message { get; }

        public PublicationStateChange(IPublication publication, PublicationState previousState, Message? message)
        {
            Publication = publication;
            PreviousState = previousState;
            Message = message;
        }

        public void Deconstruct(out IPublication publication, out PublicationState previousState, out Message? message)
        {
            publication = Publication;
            previousState = PreviousState;
            message = Message;
        }
    }
}
