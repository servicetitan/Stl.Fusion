using System.Diagnostics;
using Stl.Fusion.Messages;

namespace Stl.Fusion.Events
{
    public class PublicationDisposedEvent : PublicationStateChangedEvent
    {
        public PublicationDisposedEvent(IPublication publication, PublicationMessage? message) 
            : base(publication, message)
        {
            Debug.Assert(publication.State == PublicationState.Disposed);
        }
    }
}
