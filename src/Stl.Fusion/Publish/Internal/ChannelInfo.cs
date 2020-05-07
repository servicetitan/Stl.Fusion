using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion.Publish.Messages;
using Stl.Text;

namespace Stl.Fusion.Publish.Internal
{
    public class ChannelInfo : IPublicationHandler
    {
        public readonly IChannel<Message> Channel;
        public readonly IPublisher Publisher;
        public readonly IPublisherImpl PublisherImpl;
        public readonly ConcurrentDictionary<Symbol, Unit> Subscriptions; 
        public object Lock => this; 

        public ChannelInfo(IChannel<Message> channel, IPublisher publisher)
        {
            Channel = channel;
            Publisher = publisher;
            PublisherImpl = (IPublisherImpl) publisher;
            Subscriptions = new ConcurrentDictionary<Symbol, Unit>();
        }

        // IPublicationHandler

        public Task OnStateChangedAsync(
            IPublication publication, PublicationState previousState, Message? message,
            CancellationToken cancellationToken) 
            => message != null 
                ? Channel.SendAsync(message, cancellationToken) 
                : Task.CompletedTask;
    }
}
