using Stl.Fusion.Publish.Messages;

namespace Stl.Fusion.Publish.Events
{
    public abstract class PublicationStateChangedEvent : PublicationEvent
    {
        public Message? Message { get; }

        protected PublicationStateChangedEvent(IPublication publication, Message? message)
            : base(publication) 
            => Message = message;
    }
}
