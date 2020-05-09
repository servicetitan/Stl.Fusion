using System.Threading;
using System.Threading.Tasks;
using Stl.Text;

namespace Stl.Fusion.Publish.Internal
{
    public class PublicationInfo
    {
        public Symbol Id => Publication.Id;
        public PublicationState State => Publication.State;
        public readonly IPublication Publication;
        public readonly IPublicationImpl PublicationImpl;
        public ComputedInput Input => Publication.Computed.Input;
        public readonly PublicationFactory Factory;
        public readonly CancellationTokenSource StopCts;
        public readonly CancellationToken StopToken;
        public Task PublishTask = null!;
        public volatile CancellationTokenSource? StopDelayedUnpublishCts;
        public object Lock => this; 

        public PublicationInfo(IPublication publication, PublicationFactory factory)
        {
            Publication = publication;
            PublicationImpl = (IPublicationImpl) publication;
            Factory = factory;
            StopCts = new CancellationTokenSource();
            StopToken = StopCts.Token;
        }
    }
}
