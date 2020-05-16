using Stl.Fusion.Bridge.Messages;

namespace Stl.Fusion.Bridge.Events
{
    public abstract class PublicationStateChangedEvent : PublicationEvent
    {
        public PublicationMessage? Message { get; }

        protected PublicationStateChangedEvent(IPublication publication, PublicationMessage? message)
            : base(publication) 
            => Message = message;
    }
}
