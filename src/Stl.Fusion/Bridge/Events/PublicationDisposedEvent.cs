using System.Diagnostics;
using Stl.Fusion.Bridge.Messages;

namespace Stl.Fusion.Bridge.Events
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
