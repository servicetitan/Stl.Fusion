using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public static class PublisherEx
    {
        public static IPublication Get(
            this IPublisher publisher, Symbol publicationId) 
            => publisher.TryGet(publicationId) ?? throw new KeyNotFoundException();

        public static async Task<(IPublication<T> Publication, IComputed<T> Computed)> PublishAsync<T>(
                this IPublisher publisher, 
                Func<CancellationToken, Task<T>> producer, 
                CancellationToken cancellationToken = default)
        {
            var computed = await Computed.CaptureAsync(producer, cancellationToken).ConfigureAwait(false);
            var publication = (IPublication<T>) publisher.Publish(computed);
            return (publication, computed);
        }
    }
}
