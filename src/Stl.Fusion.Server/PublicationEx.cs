using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server;

public static class HttpContextExt
{
    public static PublicationState? GetPublicationState(this HttpContext httpContext)
    {
        if (!httpContext.Items.TryGetValue(typeof(PublicationState), out var v))
            return null;
        return (PublicationState?) v;
    }

    public static PublicationStateInfo? GetPublicationStateInfo(this HttpContext httpContext)
    {
        if (!httpContext.Items.TryGetValue(typeof(PublicationStateInfo), out var v))
            return null;
        return (PublicationStateInfo?) v;
    }

    public static void Publish(this HttpContext httpContext, IPublication publication)
    {
        using var _ = publication.Use();
        var publicationState = publication.State;
        var computed = publicationState.UntypedComputed;
        var isConsistent = computed.IsConsistent();

        var responseHeaders = httpContext.Response.Headers;
        if (responseHeaders.ContainsKey(FusionHeaders.Publication))
            throw Errors.AlreadyPublished();
        var psi = new PublicationStateInfo(publication.Ref, computed.Version, isConsistent);
        httpContext.Items[typeof(PublicationState)] = publicationState;
        httpContext.Items[typeof(PublicationStateInfo)] = psi;
        responseHeaders[FusionHeaders.Publication] = JsonConvert.SerializeObject(psi);
    }

    public static async Task<Computed<T>> Publish<T>(
        this HttpContext httpContext,
        IPublisher publisher,
        Func<CancellationToken, Task<T>> producer,
        CancellationToken cancellationToken = default)
    {
        var p = await publisher.Publish(producer, cancellationToken).ConfigureAwait(false);
        var c = p.State.Computed;
        httpContext.Publish(p);
        return c;
    }
}
