using System.Diagnostics;
using Stl.Fusion.Messages;

namespace Stl.Fusion.Events
{
    public class PublicationUpdatedEvent : PublicationStateChangedEvent
    {
        public PublicationUpdatedEvent(IPublication publication, PublicationMessage? message) 
            : base(publication, message)
        {
            Debug.Assert(publication.State == PublicationState.Updated);
        }
    }
}
