using Stl.Fusion.Messages;

namespace Stl.Fusion.Events
{
    public abstract class PublicationStateChangedEvent : PublicationEvent
    {
        public PublicationMessage? Message { get; }

        protected PublicationStateChangedEvent(IPublication publication, PublicationMessage? message)
            : base(publication) 
            => Message = message;
    }
}
