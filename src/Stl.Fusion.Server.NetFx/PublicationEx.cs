using System.Net.Http.Headers;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using Newtonsoft.Json;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server;

public static class HttpContextExt
{
    public static T GetRequiredService<T>(this IDependencyScope dependencyScope)
    {
        var service = GetService<T>(dependencyScope);
        if (service == null)
            throw new InvalidOperationException($"Required service '{typeof(T)}' is not registered.");
        return service;
    }

    public static T GetService<T>(this IDependencyScope dependencyScope)
    {
        var service = (T)dependencyScope.GetService(typeof(T));
        return service;
    }

    public static IDependencyScope GetAppServices(this HttpActionContext httpContext)
    {
        var appServices = httpContext.RequestContext.Configuration.DependencyResolver;
        return appServices;
    }

    public static PublicationState? GetPublicationState(this HttpActionContext httpContext)
    {
        if (!httpContext.GetItems().TryGetValue(typeof(PublicationState), out var v))
            return null;
        return (PublicationState?)v;
    }

    public static PublicationStateInfo? GetPublicationStateInfo(this HttpActionContext httpContext)
    {
        if (!httpContext.GetItems().TryGetValue(typeof(PublicationStateInfo), out var v))
            return null;
        return (PublicationStateInfo?)v;
    }

    public static void Publish(this HttpActionContext httpContext, IPublication publication)
    {
        using var _ = publication.Use();
        var publicationState = publication.State;
        var computed = publicationState.UntypedComputed;
        var isConsistent = computed.IsConsistent();

        // If exception occurred, response is empty, and we can not assign publication header.
        // If JsonifyAttribute is used, it will do it.
        var responseHeaders = httpContext.Response?.Headers;
        if (responseHeaders!=null && responseHeaders.Contains(FusionHeaders.Publication))
            throw Errors.AlreadyPublished();
        var psi = new PublicationStateInfo(publication.Ref, computed.Version, isConsistent);

        var items = httpContext.GetItems();
        items[typeof(PublicationState)] = publicationState;
        items[typeof(PublicationStateInfo)] = psi;

        if (responseHeaders!=null)
            responseHeaders.AddPublicationStateInfoHeader(psi);
    }

    public static async Task<IComputed<T>> Publish<T>(
        this HttpActionContext httpActionContext,
        IPublisher publisher,
        Func<CancellationToken, Task<T>> producer,
        CancellationToken cancellationToken = default)
    {
        var p = await publisher.Publish(producer, cancellationToken).ConfigureAwait(false);
        var c = p.State.Computed;
        httpActionContext.Publish(p);
        return c;
    }

    // Private & internal methods

    internal static bool AddPublicationStateInfoHeader(this HttpResponseHeaders responseHeaders, PublicationStateInfo psi)
    {
        if (responseHeaders.Contains(FusionHeaders.Publication))
            return false;
        responseHeaders.Add(FusionHeaders.Publication, JsonConvert.SerializeObject(psi));
        return true;
    }
}
