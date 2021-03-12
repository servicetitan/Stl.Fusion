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

        // TryPublishAsync

        public static async Task<IPublication?> TryPublish(
            this IPublisher publisher,
            Func<CancellationToken, Task> producer,
            CancellationToken cancellationToken = default)
        {
            var computed = await Computed
                .Capture(producer, cancellationToken)
                .ConfigureAwait(false);
            if (computed == null)
                return null;

            var publication = publisher.Publish(computed);
            // Publication doesn't have to be "in sync" with the computed
            // we requested it for (i.e. it might still point to its older,
            // inconsistent version), so we have to update it here.
            try {
                await publication.Update(cancellationToken);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch {
                // Intended, it's fine to publish a computed w/ an error
            }
            return publication;
        }

        public static async Task<IPublication<T>?> TryPublish<T>(
            this IPublisher publisher,
            Func<CancellationToken, Task<T>> producer,
            CancellationToken cancellationToken = default)
        {
            var computed = await Computed
                .Capture(producer, cancellationToken)
                .ConfigureAwait(false);
            if (computed == null)
                return null;

            var publication = (IPublication<T>) publisher.Publish(computed);
            // Publication doesn't have to be "in sync" with the computed
            // we requested it for (i.e. it might still point to its older,
            // inconsistent version), so we have to update it here.
            try {
                await publication.Update(cancellationToken);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch {
                // Intended, it's fine to publish a computed w/ an error
            }
            return publication;
        }

        // PublishAsync

        public static async Task<IPublication> Publish(
            this IPublisher publisher,
            Func<CancellationToken, Task> producer,
            CancellationToken cancellationToken = default)
        {
            var computed = await Computed
                .Capture(producer, cancellationToken)
                .ConfigureAwait(false);

            var publication = publisher.Publish(computed);
            // Publication doesn't have to be "in sync" with the computed
            // we requested it for (i.e. it might still point to its older,
            // inconsistent version), so we have to update it here.
            try {
                await publication.Update(cancellationToken);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch {
                // Intended, it's fine to publish a computed w/ an error
            }
            return publication;
        }

        public static async Task<IPublication<T>> Publish<T>(
            this IPublisher publisher,
            Func<CancellationToken, Task<T>> producer,
            CancellationToken cancellationToken = default)
        {
            var computed = await Computed
                .Capture(producer, cancellationToken)
                .ConfigureAwait(false);

            var publication = (IPublication<T>) publisher.Publish(computed);
            // Publication doesn't have to be "in sync" with the computed
            // we requested it for (i.e. it might still point to its older,
            // inconsistent version), so we have to update it here.
            try {
                await publication.Update(cancellationToken);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch {
                // Intended, it's fine to publish a computed w/ an error
            }
            return publication;
        }
    }
}
