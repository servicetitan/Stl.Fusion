using System.Diagnostics;
using Stl.Fusion.Publish.Messages;

namespace Stl.Fusion.Publish.Events
{
    public class PublicationUpdatedEvent : PublicationStateChangedEvent
    {
        public PublicationUpdatedEvent(IPublication publication, Message? message) 
            : base(publication, message)
        {
            Debug.Assert(publication.State == PublicationState.Updated);
        }
    }
}
