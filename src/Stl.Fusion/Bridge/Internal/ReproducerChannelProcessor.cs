using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion.Bridge.Messages;
using Stl.OS;

namespace Stl.Fusion.Bridge.Internal
{
    public class ReproducerChannelProcessor : AsyncProcessBase
    {
        public readonly Channel<PublicationMessage> Channel;
        public readonly IReproducer Reproducer;
        public readonly IReproducerImpl ReproducerImpl;
        protected object Lock => new object();  

        public ReproducerChannelProcessor(Channel<PublicationMessage> channel, IReproducer reproducer)
        {
            Channel = channel;
            Reproducer = reproducer;
            ReproducerImpl = (IReproducerImpl) reproducer;
        }

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            try {
                var reader = Channel.Reader;
                while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                    if (!reader.TryRead(out var message))
                        continue;
                    await OnMessageAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                await DisposeAsync().ConfigureAwait(false);
            }
        }

        protected virtual Task OnMessageAsync(PublicationMessage message, CancellationToken cancellationToken)
        {
            switch (message) {
            case SubscribeMessage _:
            case UnsubscribeMessage _:
                // Subscribe & unsubscribe messages are ignored
                break;
            }
            return Task.CompletedTask;
        }
    }
}
