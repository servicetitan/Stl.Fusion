namespace Stl.Fusion.Bridge;

public static class PublisherExt
{
    public static IPublication Get(
        this IPublisher publisher, Symbol publicationId)
        => publisher.Get(publicationId) ?? throw new KeyNotFoundException();

    // TryPublish

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
            await publication.Update(cancellationToken).ConfigureAwait(false);
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
            await publication.Update(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch {
            // Intended, it's fine to publish a computed w/ an error
        }
        return publication;
    }

#if NETSTANDARD2_0

    public static async Task<IPublication> Publish(
        this IPublisher publisher,
        IComputed computed,
        CancellationToken cancellationToken = default)
    {
        if (computed==null)
            throw new ArgumentNullException(nameof(computed));

        var publication = publisher.Publish(computed);
        // Publication doesn't have to be "in sync" with the computed
        // we requested it for (i.e. it might still point to its older,
        // inconsistent version), so we have to update it here.
        try {
            await publication.Update(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch {
            // Intended, it's fine to publish a computed w/ an error
        }
        return publication;
    }

#endif

    // Publish

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
            await publication.Update(cancellationToken).ConfigureAwait(false);
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
            await publication.Update(cancellationToken).ConfigureAwait(false);
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
