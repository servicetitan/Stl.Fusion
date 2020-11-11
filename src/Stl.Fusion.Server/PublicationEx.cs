using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server
{
    public static class HttpContextEx
    {
        public static IPublicationState? GetPublicationState(this HttpContext httpContext)
        {
            if (!httpContext.Items.TryGetValue(typeof(IPublicationState), out var v))
                return null;
            return (IPublicationState?) v;
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
            var computed = publicationState.Computed;
            var isConsistent = computed.IsConsistent();

            var responseHeaders = httpContext.Response.Headers;
            if (responseHeaders.ContainsKey(FusionHeaders.Publication))
                throw Errors.AlreadyPublished();
            var psi = new PublicationStateInfo(publication.Ref, computed.Version, isConsistent);
            httpContext.Items[typeof(IPublicationState)] = publicationState;
            httpContext.Items[typeof(PublicationStateInfo)] = psi;
            responseHeaders[FusionHeaders.Publication] = JsonConvert.SerializeObject(psi);
        }

        public static async Task<IComputed<T>> PublishAsync<T>(
            this HttpContext httpContext, IPublisher publisher,
            Func<CancellationToken, Task<T>> producer,
            CancellationToken cancellationToken = default)
        {
            var p = await publisher.PublishAsync(producer, cancellationToken).ConfigureAwait(false);
            var c = p.State.Computed;
            httpContext.Publish(p);
            return c;
        }

        public static Task<IComputed<T>> MaybePublishAsync<T>(
            this HttpContext httpContext, IPublisher publisher,
            Func<CancellationToken, Task<T>> producer,
            CancellationToken cancellationToken = default)
        {
            var headers = httpContext.Request.Headers;
            var mustPublish = headers.TryGetValue(FusionHeaders.RequestPublication, out var _);
            return mustPublish
                ? httpContext.PublishAsync(publisher, producer, cancellationToken)
                : Computed.CaptureAsync(producer, cancellationToken);
        }
    }
}
