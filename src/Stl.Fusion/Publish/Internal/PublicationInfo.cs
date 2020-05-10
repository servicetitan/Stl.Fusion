using Stl.Text;

namespace Stl.Fusion.Publish.Internal
{
    public readonly struct PublicationInfo
    {
        public readonly IPublication Publication;
        public readonly PublicationFactory Factory;
        // Shortcuts
        public Symbol Id => Publication.Id;
        public ComputedInput Input => Publication.Computed.Input;
        public PublicationState State => Publication.State;
        public IPublicationImpl PublicationImpl => (IPublicationImpl) Publication;

        public PublicationInfo(IPublication publication, PublicationFactory factory)
        {
            Publication = publication;
            Factory = factory;
        }
    }
}
