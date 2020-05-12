using System.Diagnostics;
using Stl.Fusion.Publish.Messages;

namespace Stl.Fusion.Publish.Events
{
    public class PublicationDisposedEvent : PublicationStateChangedEvent
    {
        public PublicationDisposedEvent(IPublication publication, Message? message) 
            : base(publication, message)
        {
            Debug.Assert(publication.State == PublicationState.Disposed);
        }
    }
}
