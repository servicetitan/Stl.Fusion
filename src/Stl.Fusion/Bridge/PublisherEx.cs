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

        public static async Task<IPublication<T>> PublishAsync<T>(
                this IPublisher publisher, 
                Func<CancellationToken, Task<T>> producer, 
                CancellationToken cancellationToken = default)
        {
            var computed = await Computed
                .CaptureAsync(producer, cancellationToken)
                .ConfigureAwait(false);
            var publication = (IPublication<T>) publisher.Publish(computed);
            // Publication doesn't have to be "in sync" with the computed
            // we requested it for (i.e. it might still point to its older,
            // inconsistent version), so we have to update it here.
            await publication.UpdateAsync(cancellationToken);
            return publication;
        }
    }
}
